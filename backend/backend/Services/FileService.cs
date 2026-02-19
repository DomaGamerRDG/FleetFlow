using backend.Services.Interfaces;

namespace backend.Services
{
    public class FileService : IFileService
    {
        private readonly FlottakezeloDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FileService(FlottakezeloDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<ulong> SaveFileAsync(IFormFile file, string folder, ulong uploadedByUserId)
        {
            var storagePath = Path.Combine(_env.ContentRootPath, "storage", folder);
            if (!Directory.Exists(storagePath))
                Directory.CreateDirectory(storagePath);
            var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(storagePath, storedName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var entity = new Models.File
            {
                UploadedByUserId = uploadedByUserId,
                OriginalName = file.FileName,
                StoredName = storedName,
                MimeType = file.ContentType,
                SizeBytes = (ulong)file.Length,
                StorageProvider = "LOCAL"
            };
            _context.Files.Add(entity);
            int modifiedRows = await _context.SaveChangesAsync();
            if (modifiedRows == 0)
                throw new Exception("Failed to save file metadata");
            return entity.Id;
        }

        public async Task<(byte[] Content, string MimeType, string FileName)> GetFileAsync(ulong id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                throw new Exception("File not found");
            var path = Directory.GetFiles(Path.Combine(_env.ContentRootPath, "storage"), file.StoredName, SearchOption.AllDirectories).FirstOrDefault();
            if (path == null)
                throw new Exception("File missing from storage");
            var bytes = await File.ReadAllBytesAsync(path);
            return (bytes, file.MimeType, file.OriginalName);
        }

        public async Task DeleteFileAsync(ulong id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                throw new Exception("File not found");
            var path = Directory.GetFiles(Path.Combine(_env.ContentRootPath, "storage"), file.StoredName, SearchOption.AllDirectories).FirstOrDefault();
            if (path != null)
                File.Delete(path);
            _context.Files.Remove(file);
            await _context.SaveChangesAsync();
        }
    }
}
