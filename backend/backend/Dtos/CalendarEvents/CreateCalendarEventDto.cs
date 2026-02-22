using System.ComponentModel.DataAnnotations;

namespace backend.Dtos.CalendarEvents
{
    public class CreateCalendarEventDto
    {
        [Required, MaxLength(20)]
        public string Title { get; set; } = null!;
        [MaxLength(100)]
        public string? Description { get; set; }
        [Required]
        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
