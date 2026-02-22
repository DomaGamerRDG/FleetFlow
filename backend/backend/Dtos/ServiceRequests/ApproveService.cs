using System.ComponentModel.DataAnnotations;

namespace backend.Dtos.ServiceRequests
{
    public class ApproveService
    {
        [Required]
        public DateTime ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }
        [Required, MaxLength(50)]
        public string ServiceLocation { get; set; } = "";
        [MaxLength(50)]
        public string? AdminDecisionNote { get; set; }
    }
}
