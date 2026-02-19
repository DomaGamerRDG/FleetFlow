using backend.Models;

namespace backend.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(
            ulong userId,
            string type,
            string title,
            string message,
            ulong? relatedServiceRequestId = null
        );
        Task<List<Notification>> GetUserNotificationsAsync(ulong userId);
        Task MarkAsReadAsync(ulong notificationId, ulong userId);
        Task DeleteAsync(ulong notificationId, ulong userId);
    }
}