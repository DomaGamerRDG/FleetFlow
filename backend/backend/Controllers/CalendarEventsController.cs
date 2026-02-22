using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Dtos.CalendarEvents;
using backend.Models;

namespace backend.Controllers
{
    [Route("api/calendarevents")]
    [ApiController]
    public class CalendarEventsController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        public CalendarEventsController(FlottakezeloDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> GetCalendarEvents()
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var calendarEvents = await _context.CalendarEvents.AsNoTracking().Where(x => x.OwnerUserId == userId).Select(x => new CalendarEventDto
                {
                    EventType = x.EventType,
                    Title = x.Title,
                    Description = x.Description,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt,
                    RelatedServiceRequestId = x.RelatedServiceRequestId
                }).ToListAsync();
                return Ok(calendarEvents);
            });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> CreateCalendarEvent([FromBody] CreateCalendarEventDto dto)
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
                if (dto.EndAt != null && dto.EndAt <= dto.StartAt)
                    return BadRequest("EndAt must be greater than StartAt");
                var calendarEvent = new CalendarEvent
                {
                    OwnerUserId = userId,
                    CreatedByUserId = userId,
                    EventType = "PERSONAL_TASK",
                    Title = dto.Title,
                    Description = dto.Description,
                    StartAt = dto.StartAt,
                    EndAt = dto.EndAt
                };
                _context.CalendarEvents.Add(calendarEvent);
                int createdRows = await _context.SaveChangesAsync();
                if (createdRows == 0)
                    return StatusCode(500, "Failed to create calendar event");
                return StatusCode(201, "Calendar event created successfully");
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN,DRIVER")]
        public async Task<IActionResult> DeleteCalendarEvent(ulong id)
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
                var calendarEvent = await _context.CalendarEvents.Where(x => x.Id == id && x.OwnerUserId == userId).FirstOrDefaultAsync();
                if (calendarEvent == null)
                    return NotFound();
                if (calendarEvent.EventType == "SERVICE_APPOINTMENT" && user.Role != "ADMIN")
                    return Forbid("Not allowed to delete");
                _context.CalendarEvents.Remove(calendarEvent);
                int deletedRows = await _context.SaveChangesAsync();
                if (deletedRows == 0)
                    return StatusCode(500, "Failed to delete calendar event");
                return Ok("Calendar event deleted successfully");
            });
        }
    }
}
