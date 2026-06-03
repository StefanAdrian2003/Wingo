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

        public async Task<IActionResult> OnPostCancelAsync(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.FlightSeat)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.BaggageItems)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (booking == null)
                return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Remove baggage
                var baggage = booking.Tickets
                    .SelectMany(t => t.BaggageItems)
                    .ToList();

                _context.baggageItems.RemoveRange(baggage);

                // 2. Remove flight seat reservations
                var flightSeats = booking.Tickets
                    .Select(t => t.FlightSeat)
                    .Where(fs => fs != null)
                    .ToList();

                _context.FlightSeats.RemoveRange(flightSeats!);

                // 3. Remove tickets
                _context.Tickets.RemoveRange(booking.Tickets);

                // 4. Remove booking
                _context.Bookings.Remove(booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["StatusMessage"] = "Booking cancelled successfully.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Failed to cancel booking.";
            }

            return RedirectToPage();
        }

    }

}
