namespace backend.Dtos.Vehicles
{
    public class VehiclesQuery
    {
        public string? StringQ { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Ordering { get; set; }
    }
}
