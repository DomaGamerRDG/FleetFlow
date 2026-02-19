using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var userId = ulong.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var list = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(list);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Create(CreateNotificationDto dto)
        {
            await _notificationService.CreateAsync(
                dto.UserId,
                dto.Type,
                dto.Title,
                dto.Message,
                dto.RelatedServiceRequestId);

            return Ok();
        }

        [HttpPatch("read/{id}")]
        public async Task<IActionResult> MarkRead(ulong id)
        {
            var userId = ulong.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _notificationService.MarkAsReadAsync(id, userId);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(ulong id)
        {
            var userId = ulong.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _notificationService.DeleteAsync(id, userId);
            return NoContent();
        }
    }
}
