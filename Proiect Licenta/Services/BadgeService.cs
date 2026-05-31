using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Services
{
    public class BadgeService
    {
        private readonly ApplicationDbContext _db;
        private readonly NotificationService _notificationService;

        public BadgeService(ApplicationDbContext db, NotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        // Verifica daca userul are badge-ul
        public async Task<bool> UserHasBadgeAsync(string userId, Guid badgeId)
        {
            return await _db.UserBadges
                .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
        }

        // Ofera un badge pe baza ID-ului
        public async Task AwardBadgeAsync(string userId, Guid badgeId)
        {
            // Nu da badge dacă deja există
            if (await UserHasBadgeAsync(userId, badgeId))
                return;

            var badge = await _db.Badges
                .FirstOrDefaultAsync(b => b.Id == badgeId);

            if (badge == null)
                return;

            var ub = new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                DateOfCreation = DateTime.UtcNow
            };

            _db.UserBadges.Add(ub);

            await _db.SaveChangesAsync();

            // NOTIFICATION
            await _notificationService.CreateAsync(
                receiverId: userId,
                senderId: null,
                type: NotificationType.Badge,
                message: $"You earned the '{badge.Name}' badge!"
            );
        }

        // Ofera un badge dupa nume
        public async Task AwardBadgeByNameAsync(string userId, string badgeName)
        {
            var badge = await _db.Badges
                .FirstOrDefaultAsync(b => b.Name == badgeName);

            if (badge == null)
                return; // nu exista badge-ul

            await AwardBadgeAsync(userId, badge.Id);
        }

        // Exemplu simplu: badge-uri in functie de nr. de postari
        public async Task CheckPostingBadgesAsync(string userId)
        {
            await AwardBadgeByNameAsync(userId, "Welcome");

            int postCount = await _db.Posts
                .CountAsync(p => p.UserId == userId);

            if (postCount >= 1)
                await AwardBadgeByNameAsync(userId, "First Post");

            if (postCount >= 5)
                await AwardBadgeByNameAsync(userId, "Traveler");

            if (postCount >= 20)
                await AwardBadgeByNameAsync(userId, "Explorer");

            if (postCount >= 50)
                await AwardBadgeByNameAsync(userId, "Adventurer");

            if (postCount >= 100)
                await AwardBadgeByNameAsync(userId, "Storyteller");

            if (postCount >= 250)
                await AwardBadgeByNameAsync(userId, "Content Creator");

            if (postCount >= 500)
                await AwardBadgeByNameAsync(userId, "Master Explorer");

            if (postCount >= 1000)
                await AwardBadgeByNameAsync(userId, "Legendary Poster");

            var user = await _db.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();
            int? level = user.Level;

            if (level >= 5)
                await AwardBadgeByNameAsync(userId, "Getting Started");

            if (level >= 10)
                await AwardBadgeByNameAsync(userId, "Rising Star");

            if (level >= 20)
                await AwardBadgeByNameAsync(userId, "Challenger");

            if (level >= 50)
                await AwardBadgeByNameAsync(userId, "Veteran Explorer");

            if (level >= 100)
                await AwardBadgeByNameAsync(userId, "Legend of InfinityFit");



            int likesCount = await _db.Likes
                .CountAsync(p => p.UserId == userId);

            if (likesCount >= 1)
                await AwardBadgeByNameAsync(userId, "First Love");

            if (likesCount >= 10)
                await AwardBadgeByNameAsync(userId, "Supporter");

            if (likesCount >= 50)
                await AwardBadgeByNameAsync(userId, "Positive Vibes");

            if (likesCount >= 100)
                await AwardBadgeByNameAsync(userId, "Community Booster");

            if (likesCount >= 500)
                await AwardBadgeByNameAsync(userId, "Influencer");

            if (likesCount >= 1000)
                await AwardBadgeByNameAsync(userId, "Social Machine");

            if (likesCount >= 5000)
                await AwardBadgeByNameAsync(userId, "Infinity Reactor");



            int commentsCount = await _db.Comments
                .CountAsync(p => p.UserId == userId);

            if (commentsCount >= 1)
                await AwardBadgeByNameAsync(userId, "First Words");

            if (commentsCount >= 5)
                await AwardBadgeByNameAsync(userId, "Conversationalist");

            if (commentsCount >= 15)
                await AwardBadgeByNameAsync(userId, "Active Voice");

            if (commentsCount >= 50)
                await AwardBadgeByNameAsync(userId, "Discussion Leader");

            if (commentsCount >= 100)
                await AwardBadgeByNameAsync(userId, "Community Speaker");

            if (commentsCount >= 250)
                await AwardBadgeByNameAsync(userId, "Debater Pro");

            if (commentsCount >= 500)
                await AwardBadgeByNameAsync(userId, "Social Anchor");

            if (commentsCount >= 1000)
                await AwardBadgeByNameAsync(userId, "Voice of Infinity");

        }
    }
}
