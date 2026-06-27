using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;

namespace Proiect_Licenta.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class BookingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly NotificationService _notificationService;

        public BookingsModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // Matches 'Model.Reservations' from your cshtml layout exactly
        public IList<Reservation> Reservations { get; set; } = new List<Reservation>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            // Fetch complete reservations matching your frontend include targets
            Reservations = await _context.Reservations
                .Where(r => r.UserId == user.Id)
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Flight)
                        .ThenInclude(f => f.DepartureAirport)
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Flight)
                        .ThenInclude(f => f.ArrivalAirport)
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Flight)
                        .ThenInclude(f => f.Airline)
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Tickets)
                        .ThenInclude(t => t.BaggageItems)
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Tickets)
                        .ThenInclude(t => t.FlightSeat)
                            .ThenInclude(fs => fs.Seat)
                .OrderByDescending(r => r.DateOfCreation)
                .ToListAsync();

            return Page();
        }

        // Handles asp-page-handler="Cancel" and binds asp-route-id="@reservation.Id" cleanly
        public async Task<IActionResult> OnPostCancelAsync(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var reservation = await _context.Reservations
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Flight)
                        .ThenInclude(f => f.ArrivalAirport)
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Tickets)
                        .ThenInclude(t => t.BaggageItems)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (reservation == null)
            {
                return NotFound("Reservation not found.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var finalLeg = reservation.Bookings.OrderBy(b => b.Flight.DepartureTime).LastOrDefault()?.Flight;
                string destinationCity = finalLeg?.ArrivalAirport?.City ?? "your destination";

                // Loop through and cleanly drop all multi-leg entities (All-or-Nothing Rule)
                foreach (var booking in reservation.Bookings)
                {
                    if (booking.Tickets.Any())
                    {
                        var tickets = booking.Tickets.ToList();
                        var ticketIds = tickets.Select(t => t.Id).ToList();

                        var baggage = tickets.SelectMany(t => t.BaggageItems).ToList();
                        if (baggage.Any()) _context.BaggageItems.RemoveRange(baggage);

                        var flightSeats = await _context.FlightSeats
                            .Where(fs => fs.TicketId != null && ticketIds.Contains(fs.TicketId.Value))
                            .ToListAsync();

                        foreach (var seat in flightSeats)
                        {
                            seat.TicketId = null;
                        }

                        _context.Tickets.RemoveRange(tickets);
                    }
                }

                _context.Bookings.RemoveRange(reservation.Bookings);
                _context.Reservations.Remove(reservation);

                await _notificationService.CreateAsync(
                    receiverId: user.Id,
                    senderId: user.Id,
                    type: NotificationType.FlightCancelled,
                    message: $"Your itinerary booking to {destinationCity} was successfully cancelled.",
                    postId: null
                );

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["StatusMessage"] = "Reservation completely canceled.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"An unexpected structural error occurred: {ex.Message}");
            }

            return RedirectToPage();
        }
    }
}