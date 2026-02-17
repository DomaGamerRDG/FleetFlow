namespace backend.Dtos.FuelLogs
{
    public class CreateFuelLogDto
    {
        public DateTime Date { get; set; }
        public int OdometerKm { get; set; }
        public decimal Liters { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; } = null!;
        public string? StationName { get; set; }
        public string? LocationText { get; set; }
    }
}
