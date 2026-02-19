using backend.Dtos.FuelLogs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File missing");
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest("Only .jpg, .jpeg, .png, .pdf files are allowed for receipt");
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Receipt file size cannot exceed 5MB");
            var allowedFolders = new[] { "profiles", "fuel_receipts", "service_recepiest" };
            if (!allowedFolders.Contains(folder))
                return BadRequest("Invalid folder");
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();
            ulong userId = ulong.Parse(userIdClaim);
            var id = await _fileService.SaveFileAsync(file, folder, userId);
            return Ok(id);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> Get(ulong id)
        {
            var result = await _fileService.GetFileAsync(id);
            return File(result.Content, result.MimeType, result.FileName);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> Delete(ulong id)
        {
            await _fileService.DeleteFileAsync(id);
            return NoContent();
        }
    }
}
