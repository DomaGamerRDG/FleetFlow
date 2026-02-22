using System.ComponentModel.DataAnnotations;

namespace backend.Dtos.ServiceRequests
{
    public class EditUploadedData
    {
        public decimal DriverReportCost { get; set; }
        [MaxLength(100)]
        public string? DriverCloseNote { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
