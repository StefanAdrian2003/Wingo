using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System.Text.Json;

namespace Proiect_Licenta.Pages
{
    [Authorize]
    public class BookingPassengerModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BookingPassengerModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Flight Flight { get; set; }

        // Seats that were taken by someone else between selection and confirmation
        [TempData]
        public string ConflictedSeatsJson { get; set; }

        public List<string> ConflictedSeats { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Flight = await LoadFlight(id);
            if (Flight == null) return NotFound();

            // Restore conflicted seats warning if redirected back
            if (!string.IsNullOrEmpty(ConflictedSeatsJson))
                ConflictedSeats = JsonSerializer.Deserialize<List<string>>(ConflictedSeatsJson) ?? new();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id, [FromForm] string selectedSeats)
        {
            if (string.IsNullOrWhiteSpace(selectedSeats))
            {
                ModelState.AddModelError("", "Please select at least one seat.");
                Flight = await LoadFlight(id);
                return Page();
            }

            var seatIds = JsonSerializer.Deserialize<List<Guid>>(selectedSeats);

            if (seatIds == null || seatIds.Count == 0)
            {
                ModelState.AddModelError("", "Please select at least one seat.");
                Flight = await LoadFlight(id);
                return Page();
            }

            // Store session data in TempData for next page
            var session = new BookingSessionDto
            {
                FlightId = id,
                SelectedSeatIds = seatIds
            };

            TempData["BookingSession"] = JsonSerializer.Serialize(session);

            return RedirectToPage("/BookingBaggage");
        }

        private async Task<Flight?> LoadFlight(Guid id)
        {
            return await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft)
                    .ThenInclude(a => a.SeatSections)
                        .ThenInclude(ss => ss.Seats)
                            .ThenInclude(s => s.FlightSeats)
                .FirstOrDefaultAsync(f => f.Id == id);
        }
    }

}