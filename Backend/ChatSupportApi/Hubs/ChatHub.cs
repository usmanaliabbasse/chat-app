using Microsoft.AspNetCore.SignalR;
using ChatSupportApi.Models;
using ChatSupportApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatSupportApi.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatSupportDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ChatSupportDbContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task JoinChatSession(string sessionId)
        {
            if (!Guid.TryParse(sessionId, out var guid))
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogInformation("Client {ConnectionId} joined session {SessionId}", 
                Context.ConnectionId, sessionId);
        }

        public async Task LeaveChatSession(string sessionId)
        {
            if (!Guid.TryParse(sessionId, out var guid))
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogInformation("Client {ConnectionId} left session {SessionId}", 
                Context.ConnectionId, sessionId);
        }

        public async Task SendMessage(string sessionId, string senderId, string senderName, string message)
        {
            if (!Guid.TryParse(sessionId, out var guid))
            {
                return;
            }

            try
            {
                var chatMessage = new ChatMessage
                {
                    ChatSessionId = guid,
                    SenderId = senderId,
                    SenderName = senderName,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                // Broadcast to all clients in the session
                await Clients.Group(sessionId).SendAsync("ReceiveMessage", new
                {
                    id = chatMessage.Id,
                    senderId = chatMessage.SenderId,
                    senderName = chatMessage.SenderName,
                    message = chatMessage.Message,
                    timestamp = chatMessage.Timestamp
                });

                _logger.LogInformation("Message sent in session {SessionId} by {SenderName}", 
                    sessionId, senderName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message in session {SessionId}", sessionId);
            }
        }

        public async Task NotifyAgentAssigned(string sessionId, string agentName)
        {
            await Clients.Group(sessionId).SendAsync("AgentAssigned", agentName);
            _logger.LogInformation("Notified session {SessionId} about agent assignment", sessionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
