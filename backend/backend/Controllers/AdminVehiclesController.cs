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
            return await this.Run(async () =>
            {
                var vehiclesQuery = _context.Vehicles.AsNoTracking().Select(v => new VehiclesDto
                    {
                        Id = v.Id,
                        LicensePlate = v.LicensePlate,
                        BrandModel = v.Brand + " " + v.Model,
                        Year = v.Year ?? 0,
                        CurrentMileageKm = v.CurrentMileageKm,
                        Vin = v.Vin,
                        Status = v.Status,
                        UserEmail = v.VehicleAssignments.Where(a => a.AssignedTo == null).Select(a => a.Driver.User.Email).FirstOrDefault()!
                    });
                var q = query.StringQ?.Trim();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    vehiclesQuery = vehiclesQuery.Where(x =>
                        x.LicensePlate.Contains(q) ||
                        x.BrandModel.Contains(q) ||
                        (x.UserEmail != null && x.UserEmail.Contains(q)) ||
                        (x.Vin != null && x.Vin.Contains(q)) ||
                        x.Year.ToString().Contains(q) ||
                        x.CurrentMileageKm.ToString().Contains(q)
                    );
                }
                if (string.IsNullOrWhiteSpace(query.Status))
                    vehiclesQuery = vehiclesQuery.Where(x => x.Status == "ACTIVE" || x.Status == "MAINTENANCE");
                if (!string.IsNullOrWhiteSpace(query.Status))
                    vehiclesQuery = vehiclesQuery.Where(x => x.Status == query.Status);
                var totalCount = await vehiclesQuery.CountAsync();
                vehiclesQuery = (query.Ordering?.ToLower()) switch
                {
                    "year" => vehiclesQuery.OrderBy(x => x.Year),
                    "year_desc" => vehiclesQuery.OrderByDescending(x => x.Year),
                    "currentmileagekm" => vehiclesQuery.OrderBy(x => x.CurrentMileageKm),
                    "currentmileagekm_desc" => vehiclesQuery.OrderByDescending(x => x.CurrentMileageKm),
                    "brandmodel" => vehiclesQuery.OrderBy(x => x.BrandModel),
                    "brandmodel_desc" => vehiclesQuery.OrderByDescending(x => x.BrandModel),
                    "licenseplate" => vehiclesQuery.OrderBy(x => x.LicensePlate),
                    "licenseplate_desc" => vehiclesQuery.OrderByDescending(x => x.LicensePlate),
                    _ => vehiclesQuery.OrderByDescending(x => x.Id)
                };
                var page = query.Page < 1 ? 1 : query.Page;
                var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
                var vehicles = await vehiclesQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    data = vehicles
                });
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromBody] CreateVehiclesDto dto)
        {
            return await this.Run(async () =>
            {
                if (string.IsNullOrEmpty(dto.LicensePlate) || string.IsNullOrEmpty(dto.Model) || string.IsNullOrEmpty(dto.Brand) || string.IsNullOrEmpty(dto.Vin) || dto.Year == 0)
                    return BadRequest("LicensePlate, Model, Brand, Vin and Year are required.");
                if (await _context.Vehicles.AnyAsync(x => x.LicensePlate == dto.LicensePlate || x.Vin == dto.Vin))
                    return BadRequest("Vehicle with the same license plate or vin already exists.");
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
                return StatusCode(201, "Vehicle created successfully.");
            });
        }

        [HttpPatch("deactivate/{id}")]
        public async Task<IActionResult> DeactivateVehicle(ulong id)
        {
            return await this.Run(async () =>
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
            });
        }

        [HttpPatch("maintenance/{id}")]
        public async Task<IActionResult> SetVehicleMaintenance(ulong id)
        {
            return await this.Run(async () =>
            {
                Vehicle? vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                    return NotFound("Vehicle not found.");
                vehicle.Status = "MAINTENANCE";
                vehicle.UpdatedAt = DateTime.UtcNow;
                _context.Vehicles.Update(vehicle);
                int modifiedRows = await _context.SaveChangesAsync();
                if (modifiedRows == 0)
                    return StatusCode(500, "Failed to set vehicle to maintenance.");
                return Ok($"Vehicle with ID {id} set to maintenance successfully.");
            });
        }

        [HttpPatch("activate/{id}")]
        public async Task<IActionResult> ActivateVehicle(ulong id)
        {
            return await this.Run(async () =>
            {
                Vehicle? vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                    return NotFound("Vehicle not found.");
                vehicle.Status = "ACTIVE";
                vehicle.UpdatedAt = DateTime.UtcNow;
                _context.Vehicles.Update(vehicle);
                int modifiedRows = await _context.SaveChangesAsync();
                if (modifiedRows == 0)
                    return StatusCode(500, "Failed to activate vehicle.");
                return Ok($"Vehicle with ID {id} activated successfully.");
            });
        }

        [HttpPatch("edit/{id}")]
        public async Task<IActionResult> EditVehicle(ulong id, [FromBody] CreateVehiclesDto dto)
        {
            return await this.Run(async () =>
            {
                Vehicle? vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                    return NotFound("Vehicle not found.");
                if (!string.IsNullOrEmpty(dto.LicensePlate) && vehicle.LicensePlate != dto.LicensePlate && await _context.Vehicles.AnyAsync(x => x.LicensePlate == dto.LicensePlate))
                    return BadRequest("Vehicle with the same license plate already exists.");
                if (!string.IsNullOrEmpty(dto.Vin))
                    return BadRequest("Vehicles vin is not editable.");
                if (dto.CurrentMileageKm != 0 && dto.CurrentMileageKm < vehicle.CurrentMileageKm)
                    return BadRequest("Current mileage cannot be less than the current mileage.");
                if (!string.IsNullOrEmpty(dto.LicensePlate))
                    vehicle.LicensePlate = dto.LicensePlate;
                if (!string.IsNullOrEmpty(dto.Brand))
                    vehicle.Brand = dto.Brand;
                if (!string.IsNullOrEmpty(dto.Model))
                    vehicle.Model = dto.Model;
                if (dto.Year != 0)
                    vehicle.Year = dto.Year;
                if (dto.CurrentMileageKm != 0)
                    vehicle.CurrentMileageKm = dto.CurrentMileageKm;
                vehicle.UpdatedAt = DateTime.UtcNow;
                _context.Vehicles.Update(vehicle);
                int modifiedRows = await _context.SaveChangesAsync();
                if (modifiedRows == 0)
                    return StatusCode(500, "Failed to edit vehicle.");
                return Ok($"Vehicle with ID {id} edited successfully.");
            });
        }
    }
}
