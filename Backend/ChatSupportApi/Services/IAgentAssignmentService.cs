using ChatSupportApi.Models;

namespace ChatSupportApi.Services
{
    public interface IAgentAssignmentService
    {
        Task ProcessQueue();
        Task<Agent?> GetNextAvailableAgent();
        Task<bool> AssignChatToAgent(Guid sessionId, int agentId);
        Task<bool> CompleteChatSession(Guid sessionId);
    }
}
