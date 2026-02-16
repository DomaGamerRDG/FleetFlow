using backend.Dtos.Users;
using backend.Dtos.Vehicles;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/admin/vehicles")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class AdminVehiclesController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        public AdminVehiclesController(FlottakezeloDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicles([FromQuery] VehiclesQuery query)
        {
            var vehiclesQuery = _context.Vehicles.AsNoTracking()
                .Join(
                    _context.VehicleAssignments,
                    vehicle => vehicle.Id,
                    assignment => assignment.VehicleId,
                    (vehicle, assignment) => new { vehicle, assignment }
                )
                .Join(
                    _context.Drivers,
                    va => va.assignment.DriverId,
                    driver => driver.Id,
                    (va, driver) => new { va.vehicle, va.assignment, driver }
                )
                .Join(
                    _context.Users,
                    vad => vad.driver.UserId,
                    user => user.Id,
                    (vad, user) => new VehiclesDto
                    {
                        LicensePlate = vad.vehicle.LicensePlate,
                        BrandModel = $"{vad.vehicle.Brand} {vad.vehicle.Model}",
                        Year = vad.vehicle.Year ?? 0,
                        CurrentMileageKm = vad.vehicle.CurrentMileageKm,
                        Vin = vad.vehicle.Vin,
                        UserEmail = user.Email,
                        Status = vad.vehicle.Status
                    }
                );

            var q = query.StringQ?.Trim();
            if (!string.IsNullOrWhiteSpace(q))
            {
                vehiclesQuery = vehiclesQuery.Where(x =>
                    x.LicensePlate.Contains(q) ||
                    x.UserEmail.Contains(q) ||
                    x.BrandModel.Contains(q) ||
                    (x.Vin != null && x.Vin.Contains(q)) ||
                    x.Year.ToString().Contains(q) ||
                    x.CurrentMileageKm.ToString().Contains(q)
                );
            }

            if (query.Status == "RETIRED")
                vehiclesQuery = vehiclesQuery.Where(x => x.Status == "RETIRED");
            else if (query.Status == "ACTIVE")
                vehiclesQuery = vehiclesQuery.Where(x => x.Status == "ACTIVE");
            else
                vehiclesQuery = vehiclesQuery.Where(x => x.Status == "ACTIVE" && x.Status == "MAINTENANCE");

            var totalCount = await vehiclesQuery.CountAsync();
            vehiclesQuery = (query.Ordering?.ToLower()) switch
            {
                "year" => vehiclesQuery.OrderBy(x => x.Year),
                "year_desc" => vehiclesQuery.OrderByDescending(x => x.Year),
                "currentmileagkm" => vehiclesQuery.OrderBy(x => x.CurrentMileageKm),
                "currentmileagkm_desc" => vehiclesQuery.OrderByDescending(x => x.CurrentMileageKm),
                "brandmodel" => vehiclesQuery.OrderBy(x => x.BrandModel),
                "brandmodel_desc" => vehiclesQuery.OrderByDescending(x => x.BrandModel),
                _ => vehiclesQuery.OrderBy(x => x.Year)
            };

            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize is < 1 ? 25 : Math.Min(query.PageSize, 200);
            var vehicles = await vehiclesQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                data = vehicles
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromBody] Vehicle dto)
        {
            if (await _context.Vehicles.AnyAsync(x => x.LicensePlate == dto.LicensePlate))
                return BadRequest("Vehicle with the same license plate already exists.");
            var vehicle = new Vehicle
            {
                LicensePlate = dto.LicensePlate,
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year,
                Vin = dto.Vin,
                CurrentMileageKm = dto.CurrentMileageKm,
                Status = "ACTIVE"
            };
            _context.Vehicles.Add(vehicle);
            int createdRows = await _context.SaveChangesAsync();
            if (createdRows == 0)
                return StatusCode(500, "Failed to create vehicle.");
            return Ok(new { message = "Vehicle created successfully." });
        }

        [HttpPatch("deactivate/{id}")]
        public async Task<IActionResult> DeactivateVehicle(ulong id)
        {
            Vehicle? vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return NotFound("Vehicle not found.");
            VehicleAssignment? activeAssignment = await _context.VehicleAssignments.FirstOrDefaultAsync(x => x.VehicleId == id && x.AssignedTo == null);
            if (activeAssignment != null)
            {
                activeAssignment.AssignedTo = DateTime.UtcNow;
                _context.VehicleAssignments.Update(activeAssignment);
            }
            vehicle.Status = "RETIRED";
            vehicle.UpdatedAt = DateTime.UtcNow;
            _context.Vehicles.Update(vehicle);
            int modifiedRows = await _context.SaveChangesAsync();
            if (modifiedRows == 0)
                return StatusCode(500, "Failed to deactivate vehicle.");
            return Ok($"Vehicle with ID {id} deactivated successfully.");
        }
    }
}
