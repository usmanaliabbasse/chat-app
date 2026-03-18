using ChatSupportApi.Data;
using ChatSupportApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSupportApi.Services
{
    public class QueueManagementService : IQueueManagementService
    {
        private readonly ChatSupportDbContext _context;
        private readonly IRedisService _redisService;
        private readonly IAgentAssignmentService _agentAssignmentService;
        private readonly ILogger<QueueManagementService> _logger;

        public QueueManagementService(
            ChatSupportDbContext context,
            IRedisService redisService,
            IAgentAssignmentService agentAssignmentService,
            ILogger<QueueManagementService> logger)
        {
            _context = context;
            _redisService = redisService;
            _agentAssignmentService = agentAssignmentService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, Guid? SessionId)> CreateChatSession(string userId)
        {
            try
            {
                var currentQueueLength = await _redisService.GetQueueLength();
                var maxQueueSize = await GetMaxQueueSize();
                var currentCapacity = await GetCurrentCapacity();

                _logger.LogInformation(
                    "Queue status - Current: {Current}, Max: {Max}, Capacity: {Capacity}",
                    currentQueueLength, maxQueueSize, currentCapacity);

                // Check if queue is full
                if (currentQueueLength >= maxQueueSize)
                {
                    // Check if we can activate overflow team (during office hours)
                    if (IsOfficeHours())
                    {
                        var overflowActivated = await ActivateOverflowTeam();
                        if (overflowActivated)
                        {
                            maxQueueSize = await GetMaxQueueSize();
                            if (currentQueueLength >= maxQueueSize)
                            {
                                return (false, "Chat refused: Queue is full even with overflow team", null);
                            }
                        }
                        else
                        {
                            return (false, "Chat refused: Queue is full", null);
                        }
                    }
                    else
                    {
                        return (false, "Chat refused: Queue is full and outside office hours", null);
                    }
                }

                // Create new chat session
                var chatSession = new ChatSession
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = ChatSessionStatus.Queued,
                    CreatedAt = DateTime.UtcNow,
                    LastPollTime = DateTime.UtcNow
                };

                _context.ChatSessions.Add(chatSession);
                await _context.SaveChangesAsync();

                // Add to Redis queue
                var enqueued = await _redisService.EnqueueChatSession(chatSession.Id);
                if (!enqueued)
                {
                    chatSession.Status = ChatSessionStatus.Refused;
                    await _context.SaveChangesAsync();
                    return (false, "Failed to add to queue", null);
                }

                _logger.LogInformation("Chat session {SessionId} created and queued for user {UserId}", 
                    chatSession.Id, userId);

                // Try to assign immediately if agents are available
                _ = Task.Run(() => _agentAssignmentService.ProcessQueue());

                return (true, "OK", chatSession.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat session for user {UserId}", userId);
                return (false, "Internal error", null);
            }
        }

        public async Task<ChatSession?> GetChatSession(Guid sessionId)
        {
            return await _context.ChatSessions
                .Include(cs => cs.AssignedAgent)
                .Include(cs => cs.Messages)
                .FirstOrDefaultAsync(cs => cs.Id == sessionId);
        }

        public async Task<bool> UpdatePoll(Guid sessionId)
        {
            try
            {
                var session = await _context.ChatSessions.FindAsync(sessionId);
                if (session == null)
                {
                    return false;
                }

                session.LastPollTime = DateTime.UtcNow;
                session.MissedPollCount = 0;
                await _context.SaveChangesAsync();

                _logger.LogDebug("Poll updated for session {SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating poll for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task MonitorInactiveSessions()
        {
            try
            {
                var activeSessions = await _context.ChatSessions
                    .Where(cs => cs.Status == ChatSessionStatus.Queued || cs.Status == ChatSessionStatus.Active)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    var timeSinceLastPoll = DateTime.UtcNow - session.LastPollTime;
                    
                    // Check if 3 seconds have passed (3 missed 1-second polls)
                    if (timeSinceLastPoll.TotalSeconds >= 3)
                    {
                        session.MissedPollCount++;
                        
                        if (session.MissedPollCount >= 3)
                        {
                            _logger.LogWarning("Session {SessionId} marked as inactive due to missed polls", session.Id);
                            
                            session.Status = ChatSessionStatus.Inactive;
                            
                            // Remove from queue if still queued
                            await _redisService.RemoveFromQueue(session.Id);
                            
                            // Release agent if assigned
                            if (session.AssignedAgentId.HasValue)
                            {
                                var agent = await _context.Agents.FindAsync(session.AssignedAgentId.Value);
                                if (agent != null && agent.CurrentChatCount > 0)
                                {
                                    agent.CurrentChatCount--;
                                }
                            }
                            
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring inactive sessions");
            }
        }

        public async Task<int> GetCurrentCapacity()
        {
            var activeTeams = await _context.Teams
                .Include(t => t.Agents)
                .Where(t => t.Agents.Any(a => a.IsActive))
                .ToListAsync();

            int totalCapacity = 0;
            foreach (var team in activeTeams)
            {
                var teamCapacity = team.CalculateCapacity();
                totalCapacity += teamCapacity;
                _logger.LogDebug("Team {TeamName} capacity: {Capacity}", team.Name, teamCapacity);
            }

            return totalCapacity;
        }

        public async Task<int> GetMaxQueueSize()
        {
            var capacity = await GetCurrentCapacity();
            return (int)Math.Floor(capacity * 1.5);
        }

        public async Task<long> GetCurrentQueueLength()
        {
            return await _redisService.GetQueueLength();
        }

        private bool IsOfficeHours()
        {
            // Office hours: 8:00 - 24:00 (Day and Evening shifts)
            var currentHour = DateTime.UtcNow.Hour;
            return currentHour >= 8 && currentHour < 24;
        }

        private async Task<bool> ActivateOverflowTeam()
        {
            try
            {
                var overflowTeam = await _context.Teams
                    .Include(t => t.Agents)
                    .FirstOrDefaultAsync(t => t.IsOverflow);

                if (overflowTeam == null)
                {
                    return false;
                }

                var hasActiveAgents = overflowTeam.Agents.Any(a => a.IsActive);
                if (hasActiveAgents)
                {
                    return true; // Already activated
                }

                // Activate all overflow agents
                foreach (var agent in overflowTeam.Agents)
                {
                    agent.IsActive = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Overflow team activated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating overflow team");
                return false;
            }
        }
    }
}
