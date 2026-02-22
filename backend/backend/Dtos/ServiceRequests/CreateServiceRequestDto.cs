using System.ComponentModel.DataAnnotations;

namespace backend.Dtos.ServiceRequests
{
    public class CreateServiceRequestDto
    {
        [Required, MaxLength(25)]
        public string Title { get; set; } = null!;
        [MaxLength(150)]
        public string? Description { get; set; }
    }
}
