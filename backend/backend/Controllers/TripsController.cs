using backend.Dtos;
using backend.Dtos.FuelLogs;
using backend.Dtos.Trips;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        public TripsController(FlottakezeloDbContext context)
        {
            _context = context;
        }

        [HttpGet("admin/trips")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetTrips([FromQuery] Querry query)
        {
            return await this.Run(async () =>
            {
                var tripsQuery = _context.Trips.AsNoTracking().Include(x => x.Driver).Select(v => new TripDto
                {
                    Id = v.Id,
                    UserEmail = v.Driver.User.Email,
                    LicensePlate = v.Vehicle.LicensePlate,
                    IsDeleted = v.IsDeleted,
                    StartTime = v.StartTime,
                    Long = v.EndTime - v.StartTime,
                    StartLocation = v.StartLocation,
                    EndLocation = v.EndLocation,
                    DistanceKm = v.DistanceKm,
                    Notes = v.Notes
                });
                var q = query.StringQ?.Trim();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    tripsQuery = tripsQuery.Where(x =>
                        x.LicensePlate.Contains(q) ||
                        x.UserEmail.Contains(q) ||
                        x.StartLocation!.Contains(q) ||
                        x.EndLocation!.Contains(q) ||
                        (x.Notes != null && x.Notes.Contains(q)) ||
                        x.DistanceKm.ToString()!.Contains(q) ||
                        x.Long.ToString()!.Contains(q) ||
                        x.StartTime.ToString().Contains(q)
                    );
                }
                if (query.IsDeleted == true)
                    tripsQuery = tripsQuery.Where(x => x.IsDeleted == true);
                else
                    tripsQuery = tripsQuery.Where(x => x.IsDeleted == false);
                var totalCount = await tripsQuery.CountAsync();
                tripsQuery = (query.Ordering?.ToLower()) switch
                {
                    "long" => tripsQuery.OrderBy(x => x.Long),
                    "long_desc" => tripsQuery.OrderByDescending(x => x.Long),
                    "distance" => tripsQuery.OrderBy(x => x.DistanceKm),
                    "distance_desc" => tripsQuery.OrderByDescending(x => x.DistanceKm),
                    "starttime" => tripsQuery.OrderBy(x => x.StartTime),
                    "starttime_desc" => tripsQuery.OrderByDescending(x => x.StartTime),
                    _ => tripsQuery.OrderByDescending(x => x.Id)
                };
                var page = query.Page < 1 ? 1 : query.Page;
                var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
                var trips = await tripsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    data = trips
                });
            });
        }

        [HttpGet("trips/{userId}")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> GetTripsForUser(ulong userId, [FromQuery] Querry query)
        {
            return await this.Run(async () =>
            {
                var tripsQuery = _context.Trips.AsNoTracking().Where(x => x.Driver.UserId == userId).OrderByDescending(x => x.StartTime).Select(v => new TripDto
                {
                    Id = v.Id,
                    UserEmail = v.Driver.User.Email,
                    LicensePlate = v.Vehicle.LicensePlate,
                    IsDeleted = v.IsDeleted,
                    StartTime = v.StartTime,
                    Long = v.EndTime - v.StartTime,
                    StartLocation = v.StartLocation,
                    EndLocation = v.EndLocation,
                    DistanceKm = v.DistanceKm,
                    Notes = v.Notes
                });
                var totalCount = await tripsQuery.CountAsync();
                var page = query.Page < 1 ? 1 : query.Page;
                var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
                var trips = await tripsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    data = trips
                });
            });
        }

        [HttpPatch("admin/trips/{id}/delete")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteTrip(ulong id)
        {
            return await this.Run(async () =>
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null)
                    return NotFound("Trip not found");
                trip.IsDeleted = true;
                trip.UpdatedAt = DateTime.UtcNow;
                int modifiedRow = await _context.SaveChangesAsync();
                if (modifiedRow == 0)
                    return StatusCode(500, "Failed to delete trip");
                return Ok("Trip deleted");
            });
        }

        [HttpPatch("admin/trips/{id}/restore")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RestoreTrip(ulong id)
        {
            return await this.Run(async () =>
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null)
                    return NotFound("Trip not found");
                trip.IsDeleted = false;
                trip.UpdatedAt = DateTime.UtcNow;
                int modifiedRow = await _context.SaveChangesAsync();
                if (modifiedRow == 0)
                    return StatusCode(500, "Failed to restore trip");
                return Ok("Trip restored");
            });
        }

        [HttpPatch("fuellogs/{id}/delete")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> DeleteTripForUser(ulong id)
        {
            return await this.Run(async () =>
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null)
                    return NotFound("Trip not found");
                if (trip.CreatedAt.AddHours(24) < DateTime.UtcNow)
                    return StatusCode(500, "Only trips created within the last 24 hours can be deleted");
                trip.IsDeleted = true;
                trip.UpdatedAt = DateTime.UtcNow;
                int modifiedRow = await _context.SaveChangesAsync();
                if (modifiedRow == 0)
                    return StatusCode(500, "Failed to delete trip");
                return Ok("Trip deleted");
            });
        }

        [HttpPost("trips")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> CreateTrip(CreateTripDto dto, ulong userId)
        {
            return await this.Run(async () =>
            {
                if (dto.EndTime < dto.StartTime)
                    return BadRequest("End time cannot be before start time");
                if (dto.StartTime > DateTime.UtcNow)
                    return BadRequest("Start time cannot be in the future");
                if (dto.EndTime > DateTime.UtcNow)
                    return BadRequest("End time cannot be in the future");
                if (dto.StartTime < DateTime.UtcNow.AddDays(-7))
                    return BadRequest("Start time cannot be more than 7 days in the past");
                if (dto.DistanceKm < 0)
                    return BadRequest("Distance cannot be negative");
                if (dto.StartOdometerKm > dto.EndOdometerKm)
                    return BadRequest("Start odometer cannot be greater than end odometer");
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found");
                var assignment = await _context.VehicleAssignments.Where(x => x.DriverId == user.Driver!.Id && x.AssignedTo == null).FirstOrDefaultAsync();
                if (assignment == null)
                    return NotFound("No assigned vehicle found for the driver");
                var vehicle = assignment.Vehicle;
                if (vehicle == null)
                    return NotFound("Vehicle not found");
                if (vehicle.CurrentMileageKm < dto.StartOdometerKm)
                    return BadRequest("Start odometer cannot be greater than vehicle's current mileage");
                var trip = new Trip
                {
                    DriverId = user.Driver!.Id,
                    VehicleId = vehicle.Id,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    StartLocation = dto.StartLocation,
                    EndLocation = dto.EndLocation,
                    DistanceKm = dto.DistanceKm,
                    StartOdometerKm = dto.StartOdometerKm,
                    EndOdometerKm = dto.EndOdometerKm,
                    Notes = dto.Notes
                };
                _context.Trips.Add(trip);
                vehicle.CurrentMileageKm = dto.EndOdometerKm;
                int modifiedRow = await _context.SaveChangesAsync();
                if (modifiedRow == 0)
                    return StatusCode(500, "Failed to create trip");
                return StatusCode(201, "Trip created");
            });
        }
    }
}
