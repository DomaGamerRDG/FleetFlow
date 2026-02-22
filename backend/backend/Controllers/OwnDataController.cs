using backend.Dtos.Assignments;
using backend.Dtos.FuelLogs;
using backend.Dtos.Users;
using backend.Dtos.Vehicles;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class OwnDataController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        private readonly IFileService _fileService;
        public OwnDataController(FlottakezeloDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpGet("mine")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> GetOwnProfile()
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found");
                if (user.Role == "ADMIN")
                {
                    return Ok(new UserDto
                    {
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        ProfileImgFileId = user.ProfileImgFileId
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
                        LicenseExpiryDate = user.Driver!.LicenseExpiryDate,
                        ProfileImgFileId = user.ProfileImgFileId
                    });
                }
            });
        }

        [HttpGet("assigned-vehicle")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> GetAssignedVehicle()
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
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

        [HttpPatch("edit")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> EditOwnProfile([FromForm] EditProfileDto dto)
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var user = await _context.Users.FindAsync(userId);
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
                if (dto.File != null)
                {
                    if (user.ProfileImgFileId != null)
                        await _fileService.DeleteFileAsync(user.ProfileImgFileId.Value);
                    var newId = await _fileService.SaveFileAsync(dto.File!, "profiles", userId);
                    user.ProfileImgFileId = newId;
                }
                user.UpdatedAt = DateTime.UtcNow;
                int modifiedRows = await _context.SaveChangesAsync();
                if (modifiedRows == 0)
                    return StatusCode(500, "Failed to update profile");
                return Ok("Profile updated successfully");
            });
        }
    }
}
