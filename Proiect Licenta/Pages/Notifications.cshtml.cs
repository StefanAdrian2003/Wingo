using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class NotificationsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public NotificationsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Notification> Notifications { get; set; } = new();
        public List<Post> Posts { get; set; }

        public async Task OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
                return;

            var userId = await _db.Users
                .Where(u => u.UserName == User.Identity.Name)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            Notifications = await _db.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Post)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateOfCreation)
                .ToListAsync();

            var postIds = await _db.Notifications
                .Where(n => n.UserId == userId && n.PostId != null)
                .Select(n => n.PostId.Value)
                .Distinct()
                .ToListAsync();

            Posts = await _db.Posts
                .Where(p => postIds.Contains(p.Id))
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.User)
                .ToListAsync();

            foreach (var notification in Notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }

            await _db.SaveChangesAsync();
        }
    }
}
