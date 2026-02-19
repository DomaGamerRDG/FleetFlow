namespace backend.Services.Interfaces
{
    public interface IFileService
    {
        Task<ulong> SaveFileAsync(IFormFile file, string folder, ulong uploadedByUserId);
        Task<(byte[] Content, string MimeType, string FileName)> GetFileAsync(ulong id);
        Task DeleteFileAsync(ulong id);
    }
}
