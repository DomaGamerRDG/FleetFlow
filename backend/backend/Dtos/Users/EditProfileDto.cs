using System.ComponentModel.DataAnnotations;

namespace backend.Dtos.Users
{
    public class EditProfileDto
    {
        [MaxLength(20)]
        public string? FullName { get; set; }
        [Phone, MaxLength(15)]
        public string? Phone { get; set; }
        [MaxLength(20)]
        public string? Password { get; set; }
        [MaxLength(20)]
        public string? PasswordAgain { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public ulong? ProfilePictureId { get; set; }
    }
}
