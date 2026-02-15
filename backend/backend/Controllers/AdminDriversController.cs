using backend.Dtos;
using backend.Dtos.Users;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Threading.Tasks;

namespace backend.Controllers
{
    [Route("api/admin/drivers")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class AdminDriversController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        public AdminDriversController(FlottakezeloDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers([FromQuery] UserQuery query)
        {
            var usersQuery = _context.Users.AsNoTracking().Where(x => x.Role == "DRIVER")
                .Join(
                    _context.Drivers,
                    user => user.Id,
                    driver => driver.UserId,
                    (user, driver) => new UserDto
                    {
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        LicenseNumber = driver.LicenseNumber,
                        LicenseExpiryDate = driver.LicenseExpiryDate,
                        IsActive = user.IsActive,
                        Notes = driver.Notes
                    }
                );

            // 🔎 Szűrés
            var q = query.StringQ?.Trim();
            if (!string.IsNullOrWhiteSpace(q))
            {
                usersQuery = usersQuery.Where(x =>
                    x.FullName.Contains(q) ||
                    x.Email.Contains(q) ||
                    x.LicenseNumber.Contains(q) ||
                    (x.Phone != null && x.Phone.Contains(q))
                );
            }

            if (query.IsActiveQ == false)
                usersQuery = usersQuery.Where(x => x.IsActive == false);
            else
                usersQuery = usersQuery.Where(x => x.IsActive == true);

            // 🔥 Paging előtt megszámoljuk!
            var totalCount = await usersQuery.CountAsync();

            // Rendezés
            usersQuery = (query.Ordering?.ToLower()) switch
            {
                "fullname" => usersQuery.OrderBy(x => x.FullName),
                "fullname_desc" => usersQuery.OrderByDescending(x => x.FullName),
                "licenseexpirydate" => usersQuery.OrderBy(x => x.LicenseExpiryDate),
                "licenseexpirydate_desc" => usersQuery.OrderByDescending(x => x.LicenseExpiryDate),
                _ => usersQuery.OrderBy(x => x.FullName)
            };

            // Paging
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize is < 1 ? 25 : Math.Min(query.PageSize, 200);
            var drivers = await usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                data = drivers
            });
        }

        [HttpPatch("deactivate/{id}")]
        public async Task<IActionResult> DeactivateDriver(ulong id)
        {
            return await this.Run(async () =>
            {
                User? driver = await _context.Users.FindAsync(id);
                if (driver == null || driver.Role != "DRIVER")
                    return NotFound("Driver not found.");
                Driver? driver1 = await _context.Drivers.FirstOrDefaultAsync(x => x.UserId == id);
                List<CalendarEvent> calendarEvents = await _context.CalendarEvents.Where(x => x.CreatedByUserId == id || x.OwnerUserId == id).ToListAsync();
                driver.IsActive = false;
                _context.Users.Update(driver);
                if (calendarEvents.Count > 0)
                    _context.CalendarEvents.RemoveRange(calendarEvents);
                //notificaton
                if (driver1 != null)
                {
                    VehicleAssignment? vehicleAssignment = await _context.VehicleAssignments.FirstOrDefaultAsync(x => x.DriverId == driver1.Id && x.AssignedTo == null);
                    if (vehicleAssignment != null)
                    {
                        vehicleAssignment.AssignedTo = DateTime.UtcNow;
                        _context.VehicleAssignments.Update(vehicleAssignment);
                    }
                }
                driver.UpdatedAt = DateTime.UtcNow;
                int modifiedRows = await _context.SaveChangesAsync();
                if (modifiedRows == 0)
                    return StatusCode(500, "Failed to deactivate driver.");
                return Ok($"Driver with ID {id} deactivated successfully.");
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateDriver([FromBody] CreateDriverDto createDriverDto)
        {
            return await this.Run(async () =>
            {
                if (string.IsNullOrWhiteSpace(createDriverDto.FullName) || string.IsNullOrWhiteSpace(createDriverDto.Email) || string.IsNullOrWhiteSpace(createDriverDto.Password) || string.IsNullOrWhiteSpace(createDriverDto.LicenseNumber) || !createDriverDto.LicenseExpiryDate.HasValue || string.IsNullOrWhiteSpace(createDriverDto.Phone))
                    return BadRequest("FullName, Email, Password, LicenseNumber, LicenseExpiryDate and Phone are required.");
                if (await _context.Users.AnyAsync(x => x.Email == createDriverDto.Email))
                    return BadRequest("Email already exists.");

                User newUser = new User
                {
                    FullName = createDriverDto.FullName,
                    Email = createDriverDto.Email,
                    Phone = createDriverDto.Phone,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDriverDto.Password),
                    IsActive = true,
                    Role = "DRIVER"
                };
                _context.Users.Add(newUser);
                int createdUser = await _context.SaveChangesAsync();
                Driver newDriver = new Driver
                {
                    UserId = newUser.Id,
                    LicenseNumber = createDriverDto.LicenseNumber,
                    LicenseExpiryDate = createDriverDto.LicenseExpiryDate,
                    Notes = createDriverDto.Notes
                };
                _context.Drivers.Add(newDriver);
                int createdDriver = await _context.SaveChangesAsync();
                if (createdUser == 0 || createdDriver == 0)
                    return StatusCode(500, "Failed to create driver.");
                return StatusCode(201, $"Driver created successfully. Id: {newUser.Id}");
            });
        }

        [HttpPatch("edit/{id}")]
        public async Task<IActionResult> UpdateDriver(ulong id, [FromBody] CreateDriverDto updateDriverDto)
        {
            return await this.Run(async () =>
            {
                User? driver = await _context.Users.FindAsync(id);
                Driver? driver1 = await _context.Drivers.FirstOrDefaultAsync(x => x.UserId == id);
                if (driver == null || driver.Role != "DRIVER")
                    return NotFound("Driver not found.");
                if (!string.IsNullOrWhiteSpace(updateDriverDto.FullName))
                    driver.FullName = updateDriverDto.FullName;
                if (!string.IsNullOrWhiteSpace(updateDriverDto.Phone))
                    driver.Phone = updateDriverDto.Phone; 
                driver.UpdatedAt = DateTime.UtcNow;
                if (driver1 != null)
                {
                    if (!string.IsNullOrWhiteSpace(updateDriverDto.LicenseNumber))
                        driver1.LicenseNumber = updateDriverDto.LicenseNumber;
                    if (updateDriverDto.LicenseExpiryDate.HasValue)
                        driver1.LicenseExpiryDate =updateDriverDto.LicenseExpiryDate;
                    if (!string.IsNullOrWhiteSpace(updateDriverDto.Notes))
                        driver1.Notes = updateDriverDto.Notes;
                    _context.Drivers.Update(driver1);
                }
                _context.Users.Update(driver);
                int updatedRows = await _context.SaveChangesAsync();
                if (updatedRows == 0)
                    return StatusCode(500, "Failed to update driver.");
                return Ok($"Driver with ID {id} updated successfully.");
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDriverById(ulong id)
        {
            return await this.Run(async () =>
            {
                var driver = await _context.Users.AsNoTracking().Where(x => x.Id == id && x.Role == "DRIVER")
                    .Join(
                        _context.Drivers,
                        user => user.Id,
                        driver => driver.UserId,
                        (user, driver) => new UserDto
                        {
                            FullName = user.FullName,
                            Email = user.Email,
                            Phone = user.Phone,
                            LicenseNumber = driver.LicenseNumber,
                            LicenseExpiryDate = driver.LicenseExpiryDate,
                            IsActive = user.IsActive
                        }
                    ).FirstOrDefaultAsync();
                if (driver == null)
                    return NotFound("Driver not found.");
                return Ok(driver);
            });
        }
    }
}
