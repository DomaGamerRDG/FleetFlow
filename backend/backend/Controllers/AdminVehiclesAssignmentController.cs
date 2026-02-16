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

        [HttpGet("freedrivers")]
        public async Task<IActionResult> GetFreeDrivers()
        {
            return await this.Run(async () =>
            {
                List<string> freeDrivers = await _context.Drivers.Where(d => !_context.VehicleAssignments.Any(va => va.DriverId == d.Id)).Select(d => d.User.Email).ToListAsync();
                if (freeDrivers.Count == 0)
                    return NotFound("No free drivers found.");
                return Ok(freeDrivers);
            });
        }

        [HttpGet("freevehicles")]
        public async Task<IActionResult> GetFreeVehicles()
        {
            return await this.Run(async () =>
            {
                List<string> freeVehicles = await _context.Vehicles.Where(v => !_context.VehicleAssignments.Any(va => va.VehicleId == v.Id)).Select(v => v.LicensePlate).ToListAsync();
                if (freeVehicles.Count == 0)
                    return NotFound("No free vehicles found.");
                return Ok(freeVehicles);
            });
        }

        //[HttpPost("assign")]
        //public async Task<IActionResult> AssignVehicle(string email, string licensePlate)
        //{
        //    return await this.Run(async () =>
        //    {
        //        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(licensePlate))
        //            return BadRequest("Invalid assignment data.");
        //        bool driverExists = await _context.Users.AnyAsync(x => x.Email == email);
        //        bool vehicleExists = await _context.Vehicles.AnyAsync(v => v.LicensePlate == licensePlate);
        //        if (!driverExists || !vehicleExists)
        //            return NotFound("Driver or vehicle not found.");
        //        Driver driver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.User.Email == email);
        //        bool alreadyAssigned = await _context.VehicleAssignments.AnyAsync(va => va.DriverId == assignment.DriverId || va.VehicleId == assignment.VehicleId);
        //        if (alreadyAssigned)
        //            return Conflict("Driver or vehicle is already assigned.");
        //        _context.VehicleAssignments.Add(assignment);
        //        await _context.SaveChangesAsync();
        //        return Ok("Vehicle assigned successfully.");
        //    });
        //}

        //[HttpPost("unassign")]
        //public async Task<IActionResult> UnassignVehicle([FromBody] VehicleAssignment assignment)
        //{
        //    return await this.Run(async () =>
        //    {
        //        if (assignment == null || assignment.DriverId <= 0 || assignment.VehicleId <= 0)
        //            return BadRequest("Invalid unassignment data.");
        //        var existingAssignment = await _context.VehicleAssignments.FirstOrDefaultAsync(va => va.DriverId == assignment.DriverId && va.VehicleId == assignment.VehicleId);
        //        if (existingAssignment == null)
        //            return NotFound("Assignment not found.");
        //        _context.VehicleAssignments.Remove(existingAssignment);
        //        await _context.SaveChangesAsync();
        //        return Ok("Vehicle unassigned successfully.");
        //    });
        //}
    }
}
