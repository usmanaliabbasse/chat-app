using System.ComponentModel.DataAnnotations;

namespace ChatSupportApi.Models
{
    public enum ChatSessionStatus
    {
        Queued,
        Active,
        Completed,
        Refused,
        Inactive
    }

    public class ChatSession
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string UserId { get; set; } = string.Empty;

        public ChatSessionStatus Status { get; set; } = ChatSessionStatus.Queued;

        public int? AssignedAgentId { get; set; }
        public Agent? AssignedAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AssignedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime LastPollTime { get; set; } = DateTime.UtcNow;

        public int MissedPollCount { get; set; } = 0;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
