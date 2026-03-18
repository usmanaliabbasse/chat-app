namespace ChatSupportApi.Services
{
    public interface IRedisService
    {
        Task<bool> EnqueueChatSession(Guid sessionId);
        Task<Guid?> DequeueChatSession();
        Task<long> GetQueueLength();
        Task<bool> RemoveFromQueue(Guid sessionId);
        Task<bool> SetSessionData(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetSessionData(string key);
        Task<bool> DeleteSessionData(string key);
    }
}
