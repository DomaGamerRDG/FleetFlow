namespace backend.Dtos.Assignments
{
    public class AssignmentHistoryDto
    {
        public ulong VehicleId { get; set; }
        public string LicensePlate { get; set; } = "";
        public ulong DriverId { get; set; }
        public string DriverEmail { get; set; } = "";
        public DateTime AssignedFrom { get; set; }
        public DateTime? AssignedTo { get; set; }
    }
}
