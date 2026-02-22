using backend.Dtos;
using backend.Dtos.FuelLogs;
using backend.Dtos.ServiceRequests;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Models;

namespace backend.Controllers
{
    [Route("api/service-requests")]
    [ApiController]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        private readonly IFileService _fileService;
        private readonly INotificationService _notificationService;
        public ServiceRequestsController(FlottakezeloDbContext context, IFileService fileService, INotificationService notificationService)
        {
            _context = context;
            _fileService = fileService;
            _notificationService = notificationService;
        }

        [HttpGet("admin")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetServices([FromQuery] Querry query)
        {
            return await this.Run(async () =>
            {
                var servicesQuery = _context.ServiceRequests.AsNoTracking().Include(x => x.Driver).Select(v => new ServiceRequestDto
                {
                    Id = v.Id,
                    UserEmail = v.CreatedByDriverUser.Email,
                    LicensePlate = v.Vehicle.LicensePlate,
                    ProfileImgFileId = v.Driver.User.ProfileImgFileId,
                    Title = v.Title,
                    Description = v.Description,
                    Status = v.Status,
                    ScheduledStart = v.ScheduledStart,
                    DriverReportCost = v.DriverReportCost,
                    InvoiceFileId = v.InvoiceFileId,
                    ClosedAt = v.ClosedAt
                });
                var q = query.StringQ?.Trim();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    servicesQuery = servicesQuery.Where(x =>
                        x.LicensePlate.Contains(q) ||
                        x.UserEmail.Contains(q) ||
                        x.Title.Contains(q) ||
                        (x.Description != null && x.Description.Contains(q)) ||
                        (x.ScheduledStart != null && x.ScheduledStart.ToString()!.Contains(q)) ||
                        (x.DriverReportCost != null && x.DriverReportCost.ToString()!.Contains(q)) ||
                        (x.ClosedAt != null && x.ClosedAt.ToString()!.Contains(q))
                    );
                }
                if (query.Status == "REQUESTED")
                    servicesQuery = servicesQuery.Where(x => x.Status == "REQUESTED");
                else if (query.Status == "REJECTED")
                    servicesQuery = servicesQuery.Where(x => x.Status == "REJECTED");
                else if (query.Status == "APPROVED")
                    servicesQuery = servicesQuery.Where(x => x.Status == "APPROVED");
                else if (query.Status == "DRIVER_COST")
                    servicesQuery = servicesQuery.Where(x => x.Status == "DRIVER_COST");
                else if (query.Status == "REQUESTED_EDIT")
                    servicesQuery = servicesQuery.Where(x => x.Status == "REQUESTED_EDIT");
                else if (query.Status == "CLOSED")
                    servicesQuery = servicesQuery.Where(x => x.Status == "CLOSED");
                var totalCount = await servicesQuery.CountAsync();
                servicesQuery = (query.Ordering?.ToLower()) switch
                {
                    "scheduledstart" => servicesQuery.OrderBy(x => x.ScheduledStart),
                    "scheduledstart_desc" => servicesQuery.OrderByDescending(x => x.ScheduledStart),
                    "driverreportcost" => servicesQuery.OrderBy(x => x.DriverReportCost),
                    "driverreportcost_desc" => servicesQuery.OrderByDescending(x => x.DriverReportCost),
                    "closedat" => servicesQuery.OrderBy(x => x.ClosedAt),
                    "closedat_desc" => servicesQuery.OrderByDescending(x => x.ClosedAt),
                    _ => servicesQuery.OrderByDescending(x => x.Id)
                };
                var page = query.Page < 1 ? 1 : query.Page;
                var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
                var services = await servicesQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    data = services
                });
            });
        }

        [HttpGet("mine")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> GetServicesForUser([FromQuery] Querry query)
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var servicesQuery = _context.ServiceRequests.AsNoTracking().Where(x => x.CreatedByDriverUserId == userId).OrderByDescending(x => x.ScheduledStart).Select(v => new ServiceRequestDto
                {
                    Id = v.Id,
                    UserEmail = v.CreatedByDriverUser.Email,
                    LicensePlate = v.Vehicle.LicensePlate,
                    ProfileImgFileId = v.Driver.User.ProfileImgFileId,
                    Title = v.Title,
                    Description = v.Description,
                    Status = v.Status,
                    ScheduledStart = v.ScheduledStart,
                    DriverReportCost = v.DriverReportCost,
                    InvoiceFileId = v.InvoiceFileId,
                    ClosedAt = v.ClosedAt
                });
                var totalCount = await servicesQuery.CountAsync();
                var page = query.Page < 1 ? 1 : query.Page;
                var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
                var services = await servicesQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    data = services
                });
            });
        }

        [HttpPost]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> CreateServiceRequest([FromBody] CreateServiceRequestDto dto)
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var driver = await _context.Drivers.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == userId);
                if (driver == null)
                    return Unauthorized("Driver profile not found");
                var assignment = await _context.VehicleAssignments.Where(x => x.DriverId == driver.Id && x.AssignedTo == null).FirstOrDefaultAsync();
                if (assignment == null)
                    return NotFound("No assigned vehicle found for the driver");
                var vehicle = assignment.Vehicle;
                if (vehicle == null)
                    return NotFound("Vehicle not found");
                var serviceRequest = new ServiceRequest
                {
                    VehicleId = vehicle.Id,
                    DriverId = driver.Id,
                    CreatedByDriverUserId = userId,
                    Title = dto.Title,
                    Description = dto.Description,
                    Status = "REQUESTED"
                };
                _context.ServiceRequests.Add(serviceRequest);
                int createdRows = await _context.SaveChangesAsync();
                if (createdRows == 0)
                    return StatusCode(500, "Failed to create service request");
                return StatusCode(201, "Service requests created");
            });
        }

        [HttpDelete("cancel/{id}")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> CancelServiceRequestForUser(ulong id)
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var serviceRequest = await _context.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id && x.CreatedByDriverUserId == userId);
                if (serviceRequest == null)
                    return NotFound("Service request not found");
                if (serviceRequest.Status != "REQUESTED")
                    return BadRequest("Only service requests with REQUESTED status can be cancelled");
                _context.ServiceRequests.Remove(serviceRequest);
                int deletedRows = await _context.SaveChangesAsync();
                if (deletedRows == 0)
                    return StatusCode(500, "Failed to cancel service request");
                return Ok("Service request cancelled");
            });
        }

        [HttpPatch("reject/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RejectServiceRequest(ulong id, [FromBody] string? note)
        {
            return await this.Run(async () =>
            {
                var serviceRequest = await _context.ServiceRequests.FindAsync(id);
                if (serviceRequest == null)
                    return NotFound("Service request not found");
                if (serviceRequest.Status != "REQUESTED")
                    return BadRequest("Only service requests with REQUESTED status can be rejected");
                serviceRequest.Status = "REJECTED";
                serviceRequest.AdminUserId = ulong.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                serviceRequest.AdminDecisionNote = note;
                await _notificationService.CreateAsync(
                    serviceRequest.CreatedByDriverUserId,
                    "SERVICE_REQUEST",
                    "Service Request Rejected",
                    $"Your service request '{serviceRequest.Title}' has been rejected by the admin. Note from admin: {note}"
                );
                serviceRequest.UpdatedAt = DateTime.UtcNow;
                int updatedRows = await _context.SaveChangesAsync();
                if (updatedRows == 0)
                    return StatusCode(500, "Failed to reject service request");
                return Ok("Service request rejected");
            });
        }

        [HttpPatch("approve/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ApproveServiceRequest(ulong id, [FromBody] ApproveService dto)
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                if (dto.ScheduledStart < DateTime.UtcNow)
                    return BadRequest("Scheduled start time cannot be in the past");
                if (dto.ScheduledEnd != null && dto.ScheduledEnd <= dto.ScheduledStart)
                    return BadRequest("Scheduled end time must be after scheduled start time");
                var serviceRequest = await _context.ServiceRequests.FindAsync(id);
                if (serviceRequest == null)
                    return NotFound("Service request not found");
                var vehicle = serviceRequest.Vehicle;
                if (vehicle == null)
                    return BadRequest("Associated vehicle not found");
                if (serviceRequest.Status != "REQUESTED")
                    return BadRequest("Only service requests with REQUESTED status can be approved");
                serviceRequest.Status = "APPROVED";
                serviceRequest.AdminUserId = userId;
                serviceRequest.AdminDecisionNote = dto.AdminDecisionNote;
                serviceRequest.ScheduledStart = dto.ScheduledStart;
                serviceRequest.ScheduledEnd = dto.ScheduledEnd;
                serviceRequest.ServiceLocation = dto.ServiceLocation;
                serviceRequest.UpdatedAt = DateTime.UtcNow;
                vehicle.Status = "MAINTENANCE";
                vehicle.UpdatedAt = DateTime.UtcNow;
                await _notificationService.CreateAsync(
                    serviceRequest.CreatedByDriverUserId,
                    "SERVICE_REQUEST",
                    "Service Request Approved",
                    $"Your service request '{serviceRequest.Title}' has been approved by the admin. Scheduled start: {dto.ScheduledStart}, (Scheduled end: {dto.ScheduledEnd}), Service location: {dto.ServiceLocation}. Note from admin: {dto.AdminDecisionNote}"
                );
                var calendarEvent = new CalendarEvent
                {
                    OwnerUserId = serviceRequest.CreatedByDriverUserId,
                    CreatedByUserId = userId,
                    EventType = "SERVICE_APPOINTMENT",
                    Title = "Service",
                    Description = dto.ServiceLocation,
                    StartAt = dto.ScheduledStart,
                    EndAt = dto.ScheduledEnd,
                    RelatedServiceRequestId = serviceRequest.Id
                };
                _context.CalendarEvents.Add(calendarEvent);
                int updatedRows = await _context.SaveChangesAsync();
                if (updatedRows == 0)
                    return StatusCode(500, "Failed to approve service request");
                return Ok("Service request approved");
            });
        }

        [HttpPatch("upload-details/{id}")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> UploadServiceDetails(ulong id, [FromForm] UploadServiceDetailsDto dto)
        {
            return await this.Run(async () =>
            {
                if (dto.DriverReportCost < 0)
                    return BadRequest("Driver report cost cannot be negative");
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var serviceRequest = await _context.ServiceRequests.FindAsync(id);
                if (serviceRequest == null)
                    return NotFound("Service request not found");
                if (serviceRequest.CreatedByDriverUserId != userId)
                    return Forbid("You are not the creator of this service request");
                if (serviceRequest.Status != "APPROVED")
                    return BadRequest("Only service requests with APPROVED status can have details uploaded");
                serviceRequest.DriverReportCost = dto.DriverReportCost;
                if (dto.File != null)
                {
                    var fileId = await _fileService.SaveFileAsync(dto.File, "service_requests", userId);
                    serviceRequest.InvoiceFileId = fileId;
                }
                serviceRequest.DriverCloseNote = dto.DriverCloseNote;
                serviceRequest.UpdatedAt = DateTime.UtcNow;
                serviceRequest.Status = "DRIVER_COST";
                await _notificationService.CreateAsync(
                    serviceRequest.AdminUserId ?? 0,
                    "SERVICE_REQUEST",
                    "Service Request Details Uploaded",
                    $"The driver has uploaded details for the service request '{serviceRequest.Title}'. Driver report cost: {dto.DriverReportCost}. Driver's note: {dto.DriverCloseNote}"
                );
                int updatedRows = await _context.SaveChangesAsync();
                if (updatedRows == 0)
                    return StatusCode(500, "Failed to upload service details");
                return Ok("Service details uploaded");
            });
        }

        [HttpPatch("edit-uploaded-data/{id}")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> EditUploadedData(ulong id, [FromForm] EditUploadedData dto)
        {
            return await this.Run(async () =>
            {
                if (dto.DriverReportCost < 0)
                    return BadRequest("Driver report cost cannot be negative");
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var serviceRequest = await _context.ServiceRequests.FindAsync(id);
                if (serviceRequest == null)
                    return NotFound("Service request not found");
                if (serviceRequest.CreatedByDriverUserId != userId)
                    return Forbid("You are not the creator of this service request");
                if (serviceRequest.Status != "DRIVER_COST")
                    return BadRequest("Only service requests with DRIVER_COST status can have details edited");
                if (dto.DriverReportCost != 0)
                    serviceRequest.DriverReportCost = dto.DriverReportCost;
                if (dto.File != null)
                {
                    if (serviceRequest.InvoiceFileId != null)
                        await _fileService.DeleteFileAsync(serviceRequest.InvoiceFileId.Value);
                    var newId = await _fileService.SaveFileAsync(dto.File, "service_requests", userId);
                    serviceRequest.InvoiceFileId = newId;
                }
                if (dto.DriverCloseNote != null)
                    serviceRequest.DriverCloseNote = dto.DriverCloseNote;
                serviceRequest.UpdatedAt = DateTime.UtcNow;
                await _notificationService.CreateAsync(
                    serviceRequest.AdminUserId ?? 0,
                    "SERVICE_REQUEST",
                    "Service Request Details Edited",
                    $"The driver has edited the uploaded details for the service request '{serviceRequest.Title}'. Driver report cost: {dto.DriverReportCost}. Driver's note: {dto.DriverCloseNote}"
                );
                int updatedRows = await _context.SaveChangesAsync();
                if (updatedRows == 0)
                    return StatusCode(500, "Failed to edit uploaded service details");
                return Ok("Uploaded service details edited");
            });
        }

        [HttpPatch("close/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CloseServiceRequest(ulong id)
        {
            return await this.Run(async () =>
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized();
                ulong userId = ulong.Parse(userIdClaim);
                var serviceRequest = await _context.ServiceRequests.FindAsync(id);
                if (serviceRequest == null)
                    return NotFound("Service request not found");
                if (serviceRequest.Status != "DRIVER_COST")
                    return BadRequest("Only service requests with DRIVER_COST status can be closed");
                if (serviceRequest.AdminUserId != userId)
                    return Forbid("You are not the admin assigned to this service request");
                serviceRequest.Status = "CLOSED";
                serviceRequest.ClosedAt = DateTime.UtcNow;
                serviceRequest.Vehicle.Status = "ACTIVE";
                await _notificationService.CreateAsync(
                    serviceRequest.CreatedByDriverUserId,
                    "SERVICE_REQUEST",
                    "Service Request Closed",
                    $"Your service request '{serviceRequest.Title}' has been closed by the admin"
                );
                int updatedRows = await _context.SaveChangesAsync();
                if (updatedRows == 0)
                    return StatusCode(500, "Failed to close service request");
                return Ok("Service request closed");
            });
        }
    }
}
