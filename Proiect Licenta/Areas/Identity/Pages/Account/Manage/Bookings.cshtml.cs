using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Areas.Identity.Pages.Account.Manage
{
    public class BookingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public BookingsModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Booking> Bookings { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            Bookings = await _context.Bookings
                .Where(b => b.UserId == user.Id)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.ArrivalAirport)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.BaggageItems)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.FlightSeat)  // one-to-one
                        .ThenInclude(fs => fs.Seat)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return Page();
        }
    }
}
