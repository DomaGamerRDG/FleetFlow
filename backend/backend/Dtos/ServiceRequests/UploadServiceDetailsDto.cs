using System.ComponentModel.DataAnnotations;

namespace backend.Dtos.ServiceRequests
{
    public class UploadServiceDetailsDto
    {
        [Required]
        public decimal DriverReportCost { get; set; }
        [MaxLength(100)]
        public string? DriverCloseNote { get; set; }
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
