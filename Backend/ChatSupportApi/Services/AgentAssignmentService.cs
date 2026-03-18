using ChatSupportApi.Data;
using ChatSupportApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSupportApi.Services
{
    public class AgentAssignmentService : IAgentAssignmentService
    {
        private readonly ChatSupportDbContext _context;
        private readonly IRedisService _redisService;
        private readonly ILogger<AgentAssignmentService> _logger;
        private static int _lastAssignedAgentId = 0;
        private static readonly object _lock = new object();

        public AgentAssignmentService(
            ChatSupportDbContext context,
            IRedisService redisService,
            ILogger<AgentAssignmentService> logger)
        {
            _context = context;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task ProcessQueue()
        {
            try
            {
                var queueLength = await _redisService.GetQueueLength();
                if (queueLength == 0)
                {
                    return;
                }

                _logger.LogInformation("Processing queue with {Count} sessions", queueLength);

                while (await _redisService.GetQueueLength() > 0)
                {
                    var agent = await GetNextAvailableAgent();
                    if (agent == null)
                    {
                        _logger.LogInformation("No available agents, stopping queue processing");
                        break;
                    }

                    var sessionId = await _redisService.DequeueChatSession();
                    if (!sessionId.HasValue)
                    {
                        break;
                    }

                    var assigned = await AssignChatToAgent(sessionId.Value, agent.Id);
                    if (!assigned)
                    {
                        _logger.LogWarning("Failed to assign session {SessionId} to agent {AgentId}", 
                            sessionId, agent.Id);
                        // Re-queue the session
                        await _redisService.EnqueueChatSession(sessionId.Value);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue");
            }
        }

        public async Task<Agent?> GetNextAvailableAgent()
        {
            try
            {
                // Get all active agents who can take chats
                var availableAgents = await _context.Agents
                    .Include(a => a.Team)
                    .Where(a => a.IsActive && !a.IsShiftEnding)
                    .ToListAsync();

                // Filter agents who haven't reached their capacity
                availableAgents = availableAgents
                    .Where(a => a.CurrentChatCount < a.GetMaxConcurrency())
                    .ToList();

                if (!availableAgents.Any())
                {
                    return null;
                }

                // Group by seniority and sort by priority (Junior first)
                var agentsBySeniority = availableAgents
                    .GroupBy(a => a.Seniority)
                    .OrderBy(g => g.Key.GetPriority())
                    .ToList();

                // Implement round-robin within each seniority level
                lock (_lock)
                {
                    foreach (var seniorityGroup in agentsBySeniority)
                    {
                        var agents = seniorityGroup.OrderBy(a => a.Id).ToList();
                        
                        // Find the next agent in round-robin fashion
                        var startIndex = 0;
                        if (_lastAssignedAgentId > 0)
                        {
                            var lastIndex = agents.FindIndex(a => a.Id == _lastAssignedAgentId);
                            if (lastIndex >= 0)
                            {
                                startIndex = (lastIndex + 1) % agents.Count;
                            }
                        }

                        // Try to find an agent starting from the round-robin position
                        for (int i = 0; i < agents.Count; i++)
                        {
                            var index = (startIndex + i) % agents.Count;
                            var agent = agents[index];
                            
                            if (agent.CurrentChatCount < agent.GetMaxConcurrency())
                            {
                                _lastAssignedAgentId = agent.Id;
                                _logger.LogInformation(
                                    "Selected agent {AgentName} (ID: {AgentId}, Seniority: {Seniority}, Current: {Current}/{Max})",
                                    agent.Name, agent.Id, agent.Seniority, agent.CurrentChatCount, agent.GetMaxConcurrency());
                                return agent;
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next available agent");
                return null;
            }
        }

        public async Task<bool> AssignChatToAgent(Guid sessionId, int agentId)
        {
            try
            {
                var session = await _context.ChatSessions.FindAsync(sessionId);
                var agent = await _context.Agents.FindAsync(agentId);

                if (session == null || agent == null)
                {
                    return false;
                }

                // Check if agent can still take chats
                if (!agent.CanTakeChat())
                {
                    _logger.LogWarning("Agent {AgentId} cannot take chat", agentId);
                    return false;
                }

                session.AssignedAgentId = agentId;
                session.Status = ChatSessionStatus.Active;
                session.AssignedAt = DateTime.UtcNow;

                agent.CurrentChatCount++;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Assigned session {SessionId} to agent {AgentName} (Count: {Count}/{Max})",
                    sessionId, agent.Name, agent.CurrentChatCount, agent.GetMaxConcurrency());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning chat {SessionId} to agent {AgentId}", sessionId, agentId);
                return false;
            }
        }

        public async Task<bool> CompleteChatSession(Guid sessionId)
        {
            try
            {
                var session = await _context.ChatSessions
                    .Include(cs => cs.AssignedAgent)
                    .FirstOrDefaultAsync(cs => cs.Id == sessionId);

                if (session == null)
                {
                    return false;
                }

                session.Status = ChatSessionStatus.Completed;
                session.CompletedAt = DateTime.UtcNow;

                if (session.AssignedAgent != null && session.AssignedAgent.CurrentChatCount > 0)
                {
                    session.AssignedAgent.CurrentChatCount--;
                    _logger.LogInformation(
                        "Agent {AgentName} chat count decreased to {Count}",
                        session.AssignedAgent.Name, session.AssignedAgent.CurrentChatCount);
                }

                await _context.SaveChangesAsync();

                // Try to process queue again as agent is now available
                _ = Task.Run(() => ProcessQueue());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing chat session {SessionId}", sessionId);
                return false;
            }
        }
    }
}
