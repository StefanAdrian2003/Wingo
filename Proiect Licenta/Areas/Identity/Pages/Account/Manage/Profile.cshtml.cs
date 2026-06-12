using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Authorization;



namespace Proiect_Licenta.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;

        public ProfileModel(UserManager<User> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // The currently logged-in user
        public User CurrentUser { get; set; }


        [BindProperty]
        public int? TotalPoints { get; set; } // optional if you want to show/update

        [BindProperty]
        public int? Level { get; set; }

        public IList<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

        public IList<Post> Posts { get; set; } = new List<Post>();

        public IList<Flight> Flights { get; set; } = new List<Flight>();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            CurrentUser = await _db.Users
                .Include(u => u.Airline)
                    .ThenInclude(a => a.Aircraft)
                        .ThenInclude(ac => ac.Flights)
                            .ThenInclude(f => f.DepartureAirport)

                .Include(u => u.Airline)
                    .ThenInclude(a => a.Aircraft)
                        .ThenInclude(ac => ac.Flights)
                            .ThenInclude(f => f.ArrivalAirport)

                .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (CurrentUser != null)
            {
                TotalPoints = CurrentUser.TotalPoints;
                Level = CurrentUser.Level;
            }

            var userId = CurrentUser.Id;

            UserBadges = await _db.UserBadges
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Badge)
                .OrderByDescending(d => d.DateOfCreation)
                .ToListAsync(); 

            Posts = await _db.Posts
                .Where(p => p.UserId == userId)
                .Include(l => l.Likes)
                .Include(c => c.Comments)
                .OrderByDescending(d => d.DateOfCreation)
                .ToListAsync();

            Flights = await _db.Flights
                .Where(f => f.AirlineId == CurrentUser.AirlineId)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.Airline)
                    .ThenInclude(a => a.User)
                .Include(f => f.Bookings)
                    .ThenInclude(b => b.User)
                .ToListAsync();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
            {
                return NotFound("User not found");
            }

            var userId = CurrentUser.Id;

            UserBadges = await _db.UserBadges
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Badge)
                .ToListAsync();

            Posts = await _db.Posts
                .Where(p => p.UserId == userId)
                .Include(l => l.Likes)
                .Include(c => c.Comments)
                .OrderBy(d => d.DateOfCreation)
                .ToListAsync();

            // Optionally update other fields here (TotalPoints, Level etc.)

            var result = await _userManager.UpdateAsync(CurrentUser);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            TempData["StatusMessage"] = "Profile updated successfully!";
            return RedirectToPage(); // Reload the page
        }

    }
}
