using backend.Models;

namespace backend.Dtos.Assignments
{
    public class AssignedToDto
    {
        public bool IsAssigned { get; set; }

        public SimpleDriverDto? AssignedDriver { get; set; }
        public SimpleVehicleDto? AssignedVehicle { get; set; }

        public List<SimpleDriverDto>? FreeDrivers { get; set; }
        public List<SimpleVehicleDto>? FreeVehicles { get; set; }
    }

    public class SimpleDriverDto
    {
        public ulong Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public DateTime? LicenseExpiryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class SimpleVehicleDto
    {
        public ulong Id { get; set; }
        public string LicensePlate { get; set; } = "";
        public string Model { get; set; } = "";
        public string Brand { get; set; } = "";
        public string? Vin { get; set; } = "";
        public string Status { get; set; } = "";
        public int? Year { get; set; }
        public int? CurrentMileageKm { get; set; }
    }
}
