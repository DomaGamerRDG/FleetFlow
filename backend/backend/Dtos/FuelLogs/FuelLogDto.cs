namespace backend.Dtos.FuelLogs
{
    public class FuelLogDto
    {
        public ulong Id { get; set; }
        public DateTime Date { get; set; }
        public string TotalCostCur { get; set; } = "";
        public decimal Liters { get; set; }
        public string? StationName { get; set; }
        public ulong? ReceiptFileId { get; set; }
        public string UserEmail { get; set; } = "";
        public string LicensePlate { get; set; } = "";
    }
}
