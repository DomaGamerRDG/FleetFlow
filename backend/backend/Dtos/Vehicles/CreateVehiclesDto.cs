namespace backend.Dtos.Vehicles
{
    public class CreateVehiclesDto
    {
        public string LicensePlate { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Model { get; set; } = "";
        public int Year { get; set; }
        public string Vin { get; set; } = "";
        public int CurrentMileageKm { get; set; }
    }
}
