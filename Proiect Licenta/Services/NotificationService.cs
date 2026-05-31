using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task CreateAsync(
            string receiverId,
            string? senderId,
            NotificationType type,
            string message,
            Guid? postId = null)
        {
            var notification = new Notification
            {
                UserId = receiverId,
                SenderId = senderId,
                Type = type,
                Message = message,
                PostId = postId
            };

            _db.Notifications.Add(notification);

            await _db.SaveChangesAsync();
        }
    }
}
