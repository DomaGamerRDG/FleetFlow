using backend.Dtos.Notifications;
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
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var list = await _notificationService.GetUserNotificationsAsync(userId);
                return Ok(list);
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            return await this.Run(async () =>
            {
                await _notificationService.CreateAsync(
                dto.UserId,
                dto.Type,
                dto.Title,
                dto.Message,
                dto.RelatedServiceRequestId);
                return Created();
            });
        }

        [HttpPatch("read")]
        public async Task<IActionResult> MarkRead()
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                await _notificationService.MarkAsReadAsync(userId);
                return NoContent();
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(ulong id)
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                await _notificationService.DeleteAsync(id, userId);
                return NoContent();
            });
        }

        [HttpGet("unread-status")]
        public async Task<IActionResult> HasUnread()
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var has = await _notificationService.HasUnreadNotifications(userId);
                return Ok(has);
            });
        }
    }
}
