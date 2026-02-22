namespace backend.Dtos.ServiceRequests
{
    public class ServiceRequestDto
    {
        public ulong Id { get; set; }
        public string LicensePlate { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public ulong? ProfileImgFileId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ScheduledStart { get; set; }
        public decimal? DriverReportCost { get; set; }
        public ulong? InvoiceFileId { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
