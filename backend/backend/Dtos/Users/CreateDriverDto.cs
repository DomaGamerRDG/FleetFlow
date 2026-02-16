namespace backend.Dtos.Users
{
    public class CreateDriverDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Notes { get; set; }
        public string LicenseNumber { get; set; } = "";
        public DateTime? LicenseExpiryDate { get; set; }
    }
}
