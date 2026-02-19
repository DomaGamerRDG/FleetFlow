using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly FlottakezeloDbContext _context;

        public NotificationService(FlottakezeloDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(ulong userId, string type, string title, string message, ulong? relatedServiceRequestId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                RelatedServiceRequestId = relatedServiceRequestId,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(ulong userId)
        {
            return await _context.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        public async Task MarkAsReadAsync(ulong notificationId, ulong userId)
        {
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null)
                throw new Exception("Notification not found");
            notif.IsRead = true;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ulong notificationId, ulong userId)
        {
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null)
                throw new Exception("Notification not found");
            _context.Notifications.Remove(notif);
            await _context.SaveChangesAsync();
        }
    }
}
