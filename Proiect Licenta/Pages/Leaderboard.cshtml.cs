using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class LeaderboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public LeaderboardModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class LeaderboardEntry
        {
            public string UserId { get; set; }
            public string Username { get; set; }
            public string? ProfilePictureUrl { get; set; }
            public int Level { get; set; }
            public long Score { get; set; }
            public bool IsCurrentUser { get; set; }
        }

        public List<LeaderboardEntry> TopPoints { get; set; } = new();
        public List<LeaderboardEntry> TopTravelers { get; set; } = new();
        public List<LeaderboardEntry> TopCreators { get; set; } = new();
        public List<LeaderboardEntry> TopBadges { get; set; } = new();
        public List<LeaderboardEntry> TopSocial { get; set; } = new();
        public List<LeaderboardEntry> TopAirlines { get; set; } = new();

        public string? CurrentUserId { get; set; }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            CurrentUserId = currentUser?.Id;

            // ── TOP POINTS ──
            TopPoints = await _context.Users
                .Where(u => !u.IsCompany)
                .OrderByDescending(u => u.TotalPoints)
                .Take(10)
                .Select(u => new LeaderboardEntry
                {
                    UserId = u.Id,
                    Username = u.UserName ?? "Unknown",
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    Level = u.Level ?? 1,
                    Score = u.TotalPoints ?? 0
                })
                .ToListAsync();

            // ── TOP TRAVELERS ──
            TopTravelers = await _context.Users
                .Where(u => !u.IsCompany)
                .OrderByDescending(u => u.FlightsBooked)
                .Take(10)
                .Select(u => new LeaderboardEntry
                {
                    UserId = u.Id,
                    Username = u.UserName ?? "Unknown",
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    Level = u.Level ?? 1,
                    Score = u.FlightsBooked
                })
                .ToListAsync();

            // ── TOP CREATORS (posts count) ──
            TopCreators = await _context.Users
                .Where(u => !u.IsCompany)
                .OrderByDescending(u => u.Posts.Count)
                .Take(10)
                .Select(u => new LeaderboardEntry
                {
                    UserId = u.Id,
                    Username = u.UserName ?? "Unknown",
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    Level = u.Level ?? 1,
                    Score = u.Posts.Count
                })
                .ToListAsync();

            // ── TOP BADGES ──
            TopBadges = await _context.Users
                .Where(u => !u.IsCompany)
                .OrderByDescending(u => u.UserBadges.Count)
                .Take(10)
                .Select(u => new LeaderboardEntry
                {
                    UserId = u.Id,
                    Username = u.UserName ?? "Unknown",
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    Level = u.Level ?? 1,
                    Score = u.UserBadges.Count
                })
                .ToListAsync();

            // ── TOP SOCIAL (likes + comments) ──
            TopSocial = await _context.Users
                .Where(u => !u.IsCompany)
                .OrderByDescending(u => u.Likes.Count + u.Comments.Count)
                .Take(10)
                .Select(u => new LeaderboardEntry
                {
                    UserId = u.Id,
                    Username = u.UserName ?? "Unknown",
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    Level = u.Level ?? 1,
                    Score = u.Likes.Count + u.Comments.Count
                })
                .ToListAsync();

            // ── TOP AIRLINES ──
            TopAirlines = await _context.Airlines
                .OrderByDescending(a => a.Flights
                    .SelectMany(f => f.Bookings)
                    .SelectMany(b => b.Tickets)
                    .Count())
                .Take(10)
                .Select(a => new LeaderboardEntry
                {
                    UserId = a.Id.ToString(),
                    Username = a.Name,
                    ProfilePictureUrl = a.LogoUrl,
                    Level = 0,
                    Score = a.Flights
                        .SelectMany(f => f.Bookings)
                        .SelectMany(b => b.Tickets)
                        .Count()
                })
                .ToListAsync();

            // mark current user in each list
            foreach (var list in new[] { TopPoints, TopTravelers, TopCreators, TopBadges, TopSocial })
                foreach (var entry in list)
                    entry.IsCurrentUser = entry.UserId == CurrentUserId;
        }
    }
}