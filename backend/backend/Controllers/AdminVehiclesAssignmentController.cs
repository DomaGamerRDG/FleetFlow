using backend.Dtos.Vehicles;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class AdminVehiclesAssignmentController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        public AdminVehiclesAssignmentController(FlottakezeloDbContext context)
        {
            _context = context;
        }

        [HttpGet("assign/driver/{userId}")]
        public async Task<ActionResult<AssignedToDto>> IsAssignedDriver(ulong userId)
        {
            var driver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(x => x.UserId == userId);
            if (driver == null || driver.User.Role == "ADMIN")
                return NotFound("Driver not found.");
            var assignment = await _context.VehicleAssignments.Include(a => a.Vehicle).Where(a => a.DriverId == driver.Id && a.AssignedTo == null).FirstOrDefaultAsync();
            if (assignment == null)
            {
                var freeVehicles = await _context.Vehicles.Where(v => v.Status == "ACTIVE" && !_context.VehicleAssignments.Any(a => a.VehicleId == v.Id && a.AssignedTo == null))
                    .Select(v => new SimpleVehicleDto
                    {
                        Id = v.Id,
                        LicensePlate = v.LicensePlate
                    }).ToListAsync();
                return Ok(new AssignedToDto
                {
                    IsAssigned = false,
                    FreeVehicles = freeVehicles
                });
            }
            return Ok(new AssignedToDto
            {
                IsAssigned = true,
                AssignedVehicle = new SimpleVehicleDto
                {
                    Id = assignment.Vehicle.Id,
                    LicensePlate = assignment.Vehicle.LicensePlate,
                    Brand = assignment.Vehicle.Brand,
                    Model = assignment.Vehicle.Model,
                    Vin = assignment.Vehicle.Vin,
                    Status = assignment.Vehicle.Status,
                    Year = assignment.Vehicle.Year ?? 0,
                    CurrentMileageKm = assignment.Vehicle.CurrentMileageKm
                }
            });
        }

        [HttpGet("assign/vehicle/{vehicleId}")]
        public async Task<ActionResult<AssignedToDto>> IsAssignedVehicle(ulong vehicleId)
        {
            var exisstingVehicles = await _context.Vehicles.AnyAsync(x => x.Id == vehicleId);
            if (!exisstingVehicles)
                return NotFound("Vehicle not found.");
            var assignment = await _context.VehicleAssignments.Include(a => a.Driver).ThenInclude(d => d.User).Where(a => a.VehicleId == vehicleId && a.AssignedTo == null).FirstOrDefaultAsync();
            if (assignment == null)
            {
                var freeDrivers = await _context.Drivers.Include(d => d.User).Where(d => d.User.IsActive == true && d.User.Role != "ADMIN" && !_context.VehicleAssignments.Any(a => a.DriverId == d.Id && a.AssignedTo == null))
                    .Select(d => new SimpleDriverDto
                    {
                        Id = d.User.Id,
                        Email = d.User.Email
                    }).ToListAsync();
                return Ok(new AssignedToDto
                {
                    IsAssigned = false,
                    FreeDrivers = freeDrivers
                });
            }
            return Ok(new AssignedToDto
            {
                IsAssigned = true,
                AssignedDriver = new SimpleDriverDto
                {
                    Id = assignment.Driver.User.Id,
                    FullName = assignment.Driver.User.FullName,
                    Email = assignment.Driver.User.Email,
                    Phone = assignment.Driver.User.Phone,
                    LicenseNumber = assignment.Driver.LicenseNumber,
                    LicenseExpiryDate = assignment.Driver.LicenseExpiryDate,
                    Notes = assignment.Driver.Notes
                }
            });
        }

        [HttpPost("assign/{userId}/{vehicleId}")]
        public async Task<IActionResult> Assign(ulong userId, ulong vehicleId)
        {
            return await this.Run(async () =>
            {
                var driver = await _context.Drivers.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == userId);
                if (driver == null)
                    return NotFound("Driver not found.");
                if (driver.User.IsActive == false)
                    return BadRequest("Driver is not active.");
                var vehicle = await _context.Vehicles.FirstOrDefaultAsync(x => x.Id == vehicleId);
                if (vehicle == null)
                    return NotFound("Vehicle not found.");
                if (vehicle.Status != "ACTIVE")
                    return BadRequest("Vehicle is not active.");
                var existingAssignment = await _context.VehicleAssignments.FirstOrDefaultAsync(va => va.DriverId == driver.Id && va.AssignedTo == null);
                if (existingAssignment != null)
                    return BadRequest("Driver is already assigned to a vehicle.");
                var existingVehicleAssignment = await _context.VehicleAssignments.FirstOrDefaultAsync(va => va.VehicleId == vehicleId && va.AssignedTo == null);
                if (existingVehicleAssignment != null)
                    return BadRequest("Vehicle is already assigned to a driver.");
                var assignment = new VehicleAssignment()
                {
                    DriverId = driver.Id,
                    VehicleId = vehicle.Id,
                    AssignedFrom = DateTime.UtcNow
                };
                _context.VehicleAssignments.Add(assignment);
                int createdRows = await _context.SaveChangesAsync();
                if (createdRows == 0)
                    return StatusCode(500, "Failed to assign vehicle.");
                return StatusCode(201, "Vehicle assigned successfully.");
            });
        }

        [HttpPatch("unassign/{userId}")]
        public async Task<IActionResult> Unassign(ulong userId)
        {
            return await this.Run(async () =>
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(x => x.UserId == userId);
                if (driver == null)
                    return NotFound("Driver not found.");
                var existingAssignment = await _context.VehicleAssignments.FirstOrDefaultAsync(va => va.DriverId == driver.Id && va.AssignedTo == null);
                if (existingAssignment == null)
                    return BadRequest("No active assignment found for this driver and vehicle.");
                existingAssignment.AssignedTo = DateTime.UtcNow;
                int updatedRows = await _context.SaveChangesAsync();
                if (updatedRows == 0)
                    return StatusCode(500, "Failed to unassign vehicle.");
                return Ok("Vehicle unassigned successfully.");
            });
        }

        [HttpGet("assignment/history/{id}")]
        public async Task<IActionResult> GetAssignmentHistory(ulong id)
        {
            var vehicleExists = await _context.Vehicles.AnyAsync(v => v.Id == id);
            if (!vehicleExists)
                return NotFound("Vehicle not found.");
            var history = await _context.VehicleAssignments.Where(a => a.VehicleId == id).OrderByDescending(a => a.AssignedFrom)
                .Select(a => new AssignmentHistoryDto
                {
                    VehicleId = a.VehicleId,
                    LicensePlate = a.Vehicle.LicensePlate,
                    DriverId = a.DriverId,
                    DriverEmail = a.Driver.User.Email,
                    AssignedFrom = a.AssignedFrom,
                    AssignedTo = a.AssignedTo
                }).ToListAsync();
            return Ok(history);
        }
    }
}
