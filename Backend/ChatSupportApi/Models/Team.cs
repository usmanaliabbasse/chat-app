using System.ComponentModel.DataAnnotations;

namespace ChatSupportApi.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public ShiftType Shift { get; set; }

        public bool IsOverflow { get; set; } = false;

        public ICollection<Agent> Agents { get; set; } = new List<Agent>();

        public int CalculateCapacity()
        {
            return (int)Math.Floor(Agents.Sum(a => 10 * a.Seniority.GetMultiplier()));
        }

        public int GetMaxQueueSize()
        {
            return (int)Math.Floor(CalculateCapacity() * 1.5);
        }

        public bool IsShiftActive()
        {
            var currentHour = DateTime.UtcNow.Hour;
            
            return Shift switch
            {
                ShiftType.Morning => currentHour >= 0 && currentHour < 8,
                ShiftType.Day => currentHour >= 8 && currentHour < 16,
                ShiftType.Evening => currentHour >= 16 && currentHour < 24,
                _ => false
            };
        }
    }
}
