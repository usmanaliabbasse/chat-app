using ChatSupportApi.Models;

namespace ChatSupportApi.Services
{
    public interface IQueueManagementService
    {
        Task<(bool Success, string Message, Guid? SessionId)> CreateChatSession(string userId);
        Task<ChatSession?> GetChatSession(Guid sessionId);
        Task<bool> UpdatePoll(Guid sessionId);
        Task MonitorInactiveSessions();
        Task<int> GetCurrentCapacity();
        Task<int> GetMaxQueueSize();
        Task<long> GetCurrentQueueLength();
    }
}
