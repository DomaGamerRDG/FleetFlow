using backend.Dtos.Assignments;
using backend.Dtos.FuelLogs;
using backend.Dtos.Users;
using backend.Dtos.Vehicles;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnDataController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        private readonly IWebHostEnvironment _env;
        public OwnDataController(FlottakezeloDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("profile/{id}")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> GetOwnProfile(ulong id)
        {
            return await this.Run(async () =>
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound("User not found");
                if (user.Role == "ADMIN")
                {
                    return Ok(new
                    {
                        user.FullName,
                        user.Email,
                        user.Phone
                    });
                }
                else
                {
                    return Ok(new UserDto
                    {
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        LicenseNumber = user.Driver!.LicenseNumber,
                        LicenseExpiryDate = user.Driver!.LicenseExpiryDate
                    });
                }
            });
        }

        [HttpGet("assigned-vehicle/{userId}")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> GetAssignedVehicle(ulong userId)
        {
            return await this.Run(async () =>
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found");
                var assignment = await _context.VehicleAssignments.Where(x => x.DriverId == user.Driver!.Id && x.AssignedTo == null).FirstOrDefaultAsync();
                if (assignment == null)
                    return NotFound("No assigned vehicle found for the driver");
                var vehicle = assignment.Vehicle;
                return Ok(new VehiclesDto
                {
                    BrandModel = vehicle.Brand + " " + vehicle.Model,
                    LicensePlate = vehicle.Model,
                    Year = vehicle.Year ?? 0,
                    CurrentMileageKm = vehicle.CurrentMileageKm,
                    Vin = vehicle.Vin,
                    Status = vehicle.Status
                });
            });
        }

        [HttpPatch("edit-profile/{id}")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> EditOwnProfile(ulong id, [FromBody] EditProfileDto dto)
        {
            return await this.Run(async () =>
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound("User not found");
                if (!string.IsNullOrEmpty(dto.FullName))
                    user.FullName = dto.FullName;
                if (!string.IsNullOrEmpty(dto.Phone))
                    user.Phone = dto.Phone;
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    if (dto.Password != dto.PasswordAgain)
                        return BadRequest("Passwords do not match");
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }
                if (dto.ProfilePicture != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    var ext = Path.GetExtension(dto.ProfilePicture.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext))
                        return BadRequest("Only .jpg, .jpeg, .png, .pdf files are allowed for receipt");
                    if (dto.ProfilePicture.Length > 5 * 1024 * 1024)
                        return BadRequest("Profile picture size cannot exceed 5MB");
                    var folderPath = Path.Combine(_env.ContentRootPath, "Uploads", "ProfilePictures");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                    var extension = Path.GetExtension(dto.ProfilePicture.FileName);
                    var uniqueFileName = Guid.NewGuid() + extension;
                    var filePath = Path.Combine(folderPath, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await dto.ProfilePicture.CopyToAsync(stream);
                    var file = new Models.File()
                    {
                        UploadedByUserId = user.Id,
                        OriginalName = dto.ProfilePicture.FileName,
                        StoredName = Path.GetFileName(uniqueFileName),
                        MimeType = dto.ProfilePicture.ContentType,
                        SizeBytes = (ulong)dto.ProfilePicture.Length,
                        StorageProvider = "LOCAL"
                    };
                    _context.Files.Add(file);
                    int createdRow = await _context.SaveChangesAsync();
                    if (createdRow == 0)
                        return StatusCode(500, "Failed to save receipt file");
                    dto.ProfilePictureId = file.Id;
                }
                int modifiedRows = await _context.SaveChangesAsync();
                if (modifiedRows == 0)
                    return StatusCode(500, "Failed to update profile");
                return Ok();
            });
        }
    }
}
