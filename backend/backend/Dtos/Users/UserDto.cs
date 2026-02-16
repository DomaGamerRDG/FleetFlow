namespace backend.Dtos.Users
{
    public class UserDto
    {
        public ulong Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; } = "";
        public bool? IsActive { get; set; }
        public string LicenseNumber { get; set; } = "";
        public DateTime? LicenseExpiryDate { get; set; }
        public string? Notes { get; set; }
    }
}
