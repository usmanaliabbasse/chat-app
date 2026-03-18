using Microsoft.AspNetCore.Mvc;
using ChatSupportApi.Services;
using ChatSupportApi.Models;

namespace ChatSupportApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IQueueManagementService _queueManagementService;
        private readonly IAgentAssignmentService _agentAssignmentService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IQueueManagementService queueManagementService,
            IAgentAssignmentService agentAssignmentService,
            ILogger<ChatController> logger)
        {
            _queueManagementService = queueManagementService;
            _agentAssignmentService = agentAssignmentService;
            _logger = logger;
        }

        [HttpPost("request")]
        public async Task<IActionResult> CreateChatRequest([FromBody] ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { message = "UserId is required" });
            }

            var result = await _queueManagementService.CreateChatSession(request.UserId);

            if (result.Success)
            {
                return Ok(new
                {
                    message = result.Message,
                    sessionId = result.SessionId,
                    status = "queued"
                });
            }

            return StatusCode(503, new { message = result.Message });
        }

        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetChatSession(Guid sessionId)
        {
            var session = await _queueManagementService.GetChatSession(sessionId);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            return Ok(new
            {
                sessionId = session.Id,
                userId = session.UserId,
                status = session.Status.ToString(),
                assignedAgent = session.AssignedAgent?.Name,
                createdAt = session.CreatedAt,
                assignedAt = session.AssignedAt,
                messages = session.Messages.Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    senderName = m.SenderName,
                    message = m.Message,
                    timestamp = m.Timestamp
                })
            });
        }

        [HttpPost("session/{sessionId}/poll")]
        public async Task<IActionResult> PollSession(Guid sessionId)
        {
            var updated = await _queueManagementService.UpdatePoll(sessionId);

            if (!updated)
            {
                return NotFound(new { message = "Session not found" });
            }

            var session = await _queueManagementService.GetChatSession(sessionId);

            return Ok(new
            {
                sessionId = session?.Id,
                status = session?.Status.ToString(),
                assignedAgent = session?.AssignedAgent?.Name
            });
        }

        [HttpPost("session/{sessionId}/complete")]
        public async Task<IActionResult> CompleteSession(Guid sessionId)
        {
            var completed = await _agentAssignmentService.CompleteChatSession(sessionId);

            if (!completed)
            {
                return NotFound(new { message = "Session not found or could not be completed" });
            }

            return Ok(new { message = "Session completed successfully" });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var capacity = await _queueManagementService.GetCurrentCapacity();
            var maxQueueSize = await _queueManagementService.GetMaxQueueSize();
            var currentQueueLength = await _queueManagementService.GetCurrentQueueLength();

            return Ok(new
            {
                currentCapacity = capacity,
                maxQueueSize = maxQueueSize,
                currentQueueLength = currentQueueLength,
                availableSlots = maxQueueSize - currentQueueLength
            });
        }
    }

    public class ChatRequestDto
    {
        public string UserId { get; set; } = string.Empty;
    }
}
