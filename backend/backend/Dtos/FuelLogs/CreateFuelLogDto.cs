using System.ComponentModel.DataAnnotations;

namespace backend.Dtos.FuelLogs
{
    public class CreateFuelLogDto
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public int OdometerKm { get; set; }
        [Required]
        public decimal Liters { get; set; }
        [Required]
        public decimal TotalCost { get; set; }
        [MaxLength(3)]
        public string Currency { get; set; } = "HUF";
        [MaxLength(10)]
        public string? StationName { get; set; }
        [MaxLength(30)]
        public string? LocationText { get; set; }
        [Required]
        public IFormFile File { get; set; } = null!;
        public ulong? ReceiptFileId { get; set; }
    }
}
