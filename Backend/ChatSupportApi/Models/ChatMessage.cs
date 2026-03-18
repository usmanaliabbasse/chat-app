using System.ComponentModel.DataAnnotations;

namespace ChatSupportApi.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public Guid ChatSessionId { get; set; }
        public ChatSession? ChatSession { get; set; }

        [Required]
        [MaxLength(200)]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
