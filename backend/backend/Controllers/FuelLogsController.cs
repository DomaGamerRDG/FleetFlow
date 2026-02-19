using backend.Dtos;
using backend.Dtos.FuelLogs;
using backend.Dtos.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Controllers
{
    [Route("api")]
    [ApiController]
    public class FuelLogsController : ControllerBase
    {
        private readonly FlottakezeloDbContext _context;
        private readonly IWebHostEnvironment _env;
        public FuelLogsController(FlottakezeloDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("admin/fuellogs")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetFuellogs([FromQuery] Querry query)
        {
            return await this.Run(async () =>
            {
                var fuellogsQuery = _context.FuelLogs.AsNoTracking().Include(x => x.Driver).Select(v => new FuelLogDto
                {
                    Id = v.Id,
                    Date = v.Date,
                    TotalCostCur = v.TotalCost.ToString() + " Ft",
                    Liters = v.Liters,
                    StationName = v.StationName,
                    ReceiptFileId = v.ReceiptFileId,
                    UserEmail = v.Driver.User.Email,
                    LicensePlate = v.Vehicle.LicensePlate,
                    IsDeleted = v.IsDeleted
                });
                var q = query.StringQ?.Trim();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    fuellogsQuery = fuellogsQuery.Where(x =>
                        x.LicensePlate.Contains(q) ||
                        x.TotalCostCur.Contains(q) ||
                        x.UserEmail.Contains(q) ||
                        (x.StationName != null && x.StationName.Contains(q)) ||
                        x.Liters.ToString().Contains(q) ||
                        x.Date.ToString().Contains(q)
                    );
                }
                if (query.IsDeleted == true)
                    fuellogsQuery = fuellogsQuery.Where(x => x.IsDeleted == true);
                else
                    fuellogsQuery = fuellogsQuery.Where(x => x.IsDeleted == false);
                var totalCount = await fuellogsQuery.CountAsync();
                fuellogsQuery = (query.Ordering?.ToLower()) switch
                {
                    "date" => fuellogsQuery.OrderBy(x => x.Date),
                    "date_desc" => fuellogsQuery.OrderByDescending(x => x.Date),
                    "totalcost" => fuellogsQuery.OrderBy(x => x.TotalCostCur),
                    "totalcost_desc" => fuellogsQuery.OrderByDescending(x => x.TotalCostCur),
                    _ => fuellogsQuery.OrderByDescending(x => x.Id)
                };
                var page = query.Page < 1 ? 1 : query.Page;
                var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
                var fuellogs = await fuellogsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    data = fuellogs
                });
            });
        }

        [HttpGet("fuellogs/{userId}")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> GetFuellogsForUser(ulong userId, [FromQuery] Querry query)
        {
            return await this.Run(async () =>
            {
                var fuellogsQuery = _context.FuelLogs.AsNoTracking().Where(x => x.Driver.UserId == userId).OrderByDescending(x => x.Date).Select(v => new FuelLogDto
                {
                    Id = v.Id,
                    Date = v.Date,
                    TotalCostCur = v.TotalCost.ToString() + "" + v.Currency,
                    Liters = v.Liters,
                    StationName = v.StationName,
                    ReceiptFileId = v.ReceiptFileId,
                    UserEmail = v.Driver.User.Email,
                    LicensePlate = v.Vehicle.LicensePlate,
                    IsDeleted = v.IsDeleted
                });
                var totalCount = await fuellogsQuery.CountAsync();
                var page = query.Page < 1 ? 1 : query.Page;
                var pageSize = query.PageSize < 1 ? 25 : Math.Min(query.PageSize, 200);
                var fuellogs = await fuellogsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    data = fuellogs
                });
            });
        }

        [HttpPatch("admin/fuellogs/{id}/delete")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteFuellog(ulong id)
        {
            return await this.Run(async () =>
            {
                var fuellog = await _context.FuelLogs.FindAsync(id);
                if (fuellog == null)
                    return NotFound("Fuellog not found");
                fuellog.IsDeleted = true;
                int modifiedRow = await _context.SaveChangesAsync();
                if (modifiedRow == 0)
                    return StatusCode(500, "Failed to delete fuellog");
                return Ok("Fuellog deleted");
            });
        }

        [HttpPatch("fuellogs/{id}/delete")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> DeleteFuellogForUser(ulong id)
        {
            return await this.Run(async () =>
            {
                var fuellog = await _context.FuelLogs.FindAsync(id);
                if (fuellog == null)
                    return NotFound("Fuellog not found");
                if (fuellog.CreatedAt.AddHours(24) < DateTime.UtcNow)
                    return StatusCode(500, "Only fuellogs created within the last 24 hours can be deleted");
                fuellog.IsDeleted = true;
                int modifiedRow = await _context.SaveChangesAsync();
                if (modifiedRow == 0)
                    return StatusCode(500, "Failed to delete fuellog");
                return Ok("Fuellog deleted");
            });
        }

        [HttpPatch("fuellogs/{id}/restore")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RestoreFuellog(ulong id)
        {
            return await this.Run(async () =>
            {
                var fuellog = await _context.FuelLogs.FindAsync(id);
                if (fuellog == null)
                    return NotFound("Fuellog not found");
                fuellog.IsDeleted = false;
                int modifiedRow = await _context.SaveChangesAsync();
                if (modifiedRow == 0)
                    return StatusCode(500, "Failed to rstore fuellog");
                return Ok("Fuellog restored");
            });
        }

        [HttpPost("fuellogs")]
        [Authorize(Roles = "DRIVER")]
        public async Task<IActionResult> CreateFuellog(CreateFuelLogDto createFuelLogDto, ulong userId)
        {
            return await this.Run(async () =>
            {
                if (createFuelLogDto.Liters <= 0)
                    return BadRequest("Liters must be greater than 0");
                if (createFuelLogDto.TotalCost <= 0)
                    return BadRequest("Total cost must be greater than 0");
                if (createFuelLogDto.Date > DateTime.UtcNow)
                    return BadRequest("Date cannot be in the future");
                if (createFuelLogDto.Date < DateTime.UtcNow.AddDays(-7))
                    return BadRequest("Date cannot be older than 7 days");
                if (createFuelLogDto.File != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    var extension = Path.GetExtension(createFuelLogDto.File.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                        return BadRequest("Only .jpg, .jpeg, .png, .pdf files are allowed for receipt");
                    if (createFuelLogDto.File.Length > 5 * 1024 * 1024)
                        return BadRequest("Receipt file size cannot exceed 5MB");
                }
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found");
                var assignment = await _context.VehicleAssignments.Where(x => x.DriverId == user.Driver!.Id && x.AssignedTo == null).FirstOrDefaultAsync();
                if (assignment == null)
                    return NotFound("No assigned vehicle found for the driver");
                var vehicle = assignment.Vehicle;
                if (vehicle == null)
                    return NotFound("Vehicle not found");
                if (createFuelLogDto.OdometerKm < vehicle.CurrentMileageKm)
                    return BadRequest("OdometerKm must be greater than or equal to the current mileage of the vehicle");
                if (createFuelLogDto.File != null)
                {
                    var folderPath = Path.Combine(_env.ContentRootPath, "Uploads", "Fuellogs");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                    var extension = Path.GetExtension(createFuelLogDto.File.FileName);
                    var uniqueFileName = Guid.NewGuid() + extension;
                    var filePath = Path.Combine(folderPath, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await createFuelLogDto.File.CopyToAsync(stream);
                    var file = new Models.File()
                    {
                        UploadedByUserId = userId,
                        OriginalName = createFuelLogDto.File.FileName,
                        StoredName = Path.GetFileName(uniqueFileName),
                        MimeType = createFuelLogDto.File.ContentType,
                        SizeBytes = (ulong)createFuelLogDto.File.Length,
                        StorageProvider = "LOCAL"
                    };
                    _context.Files.Add(file);
                    int createdRow = await _context.SaveChangesAsync();
                    if (createdRow == 0)
                        return StatusCode(500, "Failed to save receipt file");
                    createFuelLogDto.ReceiptFileId = file.Id;
                }
                var fuellog = new FuelLog
                {
                    VehicleId = vehicle.Id,
                    DriverId = user.Driver!.Id,
                    Date = createFuelLogDto.Date,
                    OdometerKm = createFuelLogDto.OdometerKm,
                    TotalCost = createFuelLogDto.TotalCost,
                    Currency = createFuelLogDto.Currency,
                    Liters = createFuelLogDto.Liters,
                    StationName = createFuelLogDto.StationName,
                    LocationText = createFuelLogDto.LocationText,
                    ReceiptFileId = createFuelLogDto.ReceiptFileId
                };
                _context.FuelLogs.Add(fuellog);
                vehicle.CurrentMileageKm = createFuelLogDto.OdometerKm;
                int createdRow1 = await _context.SaveChangesAsync();
                if (createdRow1 == 0)
                    return StatusCode(500, "Failed to create fuellog");
                return StatusCode(201, "Fuellog created");
            });
        }

        [HttpGet("fuellogs/receipt/{fileId}")]
        [Authorize(Roles = "DRIVER,ADMIN")]
        public async Task<IActionResult> GetFuellogReceipt(ulong fileId)
        {
            return await this.Run(async () =>
            {
                var file = await _context.Files.FindAsync(fileId);
                if (file == null)
                    return NotFound("File not found");
                var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "Fuellogs", file.StoredName);
                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found on server");
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                    await stream.CopyToAsync(memory);
                memory.Position = 0;
                return File(memory, file.MimeType, file.StoredName);
            });
        }
    }
}
