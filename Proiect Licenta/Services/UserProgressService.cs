using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Services
{
    public class UserProgressService
    {
        private readonly ApplicationDbContext _db;
        private readonly BadgeService _badgeService;
        private readonly VoucherService _voucherService;

        public UserProgressService(ApplicationDbContext db, VoucherService voucherService, BadgeService badgeService)
        {
            _db = db;
            _badgeService = badgeService;
            _voucherService = voucherService;
        }


        public static int GetLevelForPoints(int points)
        {
            return (int)Math.Floor(Math.Sqrt(points / 100.0)) + 1;
        }


        public async Task AddPointsAsync(User user, int pointsToAdd)
        {
            user.TotalPoints ??= 0;
            user.Level ??= 1;

            user.TotalPoints += pointsToAdd;

            int newLevel = GetLevelForPoints(user.TotalPoints.Value);

            if (newLevel > user.Level)
            {
                user.Level = newLevel;
                user.LastLevelUp = DateTime.UtcNow;

                // atribuie un voucher random
                var voucher = await _voucherService.AssignRandomVoucherAsync(user);

            }

            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            await _badgeService.CheckPostingBadgesAsync(user.Id);
        }
    }

}
