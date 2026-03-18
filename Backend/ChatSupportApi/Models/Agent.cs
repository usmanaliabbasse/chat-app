using System.ComponentModel.DataAnnotations;

namespace ChatSupportApi.Models
{
    public class Agent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Seniority Seniority { get; set; }

        public int TeamId { get; set; }
        public Team? Team { get; set; }

        public int CurrentChatCount { get; set; } = 0;

        public bool IsShiftEnding { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public int GetMaxConcurrency()
        {
            return (int)Math.Floor(10 * Seniority.GetMultiplier());
        }

        public bool CanTakeChat()
        {
            return IsActive && !IsShiftEnding && CurrentChatCount < GetMaxConcurrency();
        }
    }
}
