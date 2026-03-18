using StackExchange.Redis;
using System.Text.Json;

namespace ChatSupportApi.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private const string QueueKey = "chat:queue";

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        public async Task<bool> EnqueueChatSession(Guid sessionId)
        {
            try
            {
                await _db.ListRightPushAsync(QueueKey, sessionId.ToString());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Guid?> DequeueChatSession()
        {
            try
            {
                var value = await _db.ListLeftPopAsync(QueueKey);
                if (value.HasValue && Guid.TryParse(value, out var sessionId))
                {
                    return sessionId;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<long> GetQueueLength()
        {
            try
            {
                return await _db.ListLengthAsync(QueueKey);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<bool> RemoveFromQueue(Guid sessionId)
        {
            try
            {
                await _db.ListRemoveAsync(QueueKey, sessionId.ToString());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SetSessionData(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                return await _db.StringSetAsync(key, value, expiry);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string?> GetSessionData(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteSessionData(string key)
        {
            try
            {
                return await _db.KeyDeleteAsync(key);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
