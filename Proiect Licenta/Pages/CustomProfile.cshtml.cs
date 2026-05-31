using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class CustomProfileModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CustomProfileModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public User CurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return NotFound();

            CurrentUser = await _db.Users
                .Include(u => u.Airline)
                    .ThenInclude(a => a.Aircraft)
                        .ThenInclude(ac => ac.Flights)
                            .ThenInclude(f => f.DepartureAirport)
                .Include(u => u.Airline)
                    .ThenInclude(a => a.Aircraft)
                        .ThenInclude(ac => ac.Flights)
                            .ThenInclude(f => f.ArrivalAirport)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Likes)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.Comments)
                .Include(u => u.UserBadges)
                    .ThenInclude(ub => ub.Badge)
                .FirstOrDefaultAsync(u => u.UserName == username);

            if (CurrentUser == null)
                return NotFound();

            return Page();
        }
    }
}