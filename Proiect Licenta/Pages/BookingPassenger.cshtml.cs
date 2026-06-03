using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System.Security.Claims;
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
        public Flight? ReturnFlight { get; set; }

        public BookingSessionDto? Session { get; set; }

        // ADD THESE
        public List<string> ConflictedSeats { get; set; } = new();
        public List<string> ConflictedSeatsReturn { get; set; } = new();

        private string BookingKey =>
              $"Booking:{User.FindFirstValue(ClaimTypes.NameIdentifier)}";

        public async Task<IActionResult> OnGetAsync(Guid id, Guid? returnId)
        {
            Flight = await LoadFlight(id);
            if (Flight == null) return NotFound();

            if (returnId.HasValue)
            {
                ReturnFlight = await LoadFlight(returnId.Value);
                if (ReturnFlight == null) return NotFound();
            }

            var sessionJson = TempData.Peek(BookingKey)?.ToString();

            if (!string.IsNullOrEmpty(sessionJson))
            {
                Session = JsonSerializer.Deserialize<BookingSessionDto>(sessionJson);
            }

            if (Session != null)
            {
                // restore conflict messages
                ConflictedSeats = Session.ConflictedSeats;
                ConflictedSeatsReturn = Session.ConflictedSeatsReturn;

                // clear them so they are shown only once
                Session.ConflictedSeats.Clear();
                Session.ConflictedSeatsReturn.Clear();

                TempData[BookingKey] =
                    JsonSerializer.Serialize(Session);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id, Guid? returnId,
                                                     [FromForm] string selectedSeats,
                                                     [FromForm] string? returnSeats)
        {
            var seatIds = JsonSerializer.Deserialize<List<Guid>>(selectedSeats);
            var returnSeatIds = returnSeats != null
                ? JsonSerializer.Deserialize<List<Guid>>(returnSeats) ?? new()
                : new List<Guid>();

            if (seatIds == null || seatIds.Count == 0)
            {
                ModelState.AddModelError("", "Please select at least one seat.");
                Flight = await LoadFlight(id);
                return Page();
            }

            if (returnId.HasValue && returnSeatIds.Count != seatIds.Count)
            {
                ModelState.AddModelError("",
                    $"Please select the same number of seats for both flights.");

                Flight = await LoadFlight(id);
                ReturnFlight = await LoadFlight(returnId.Value);

                // KEEP SESSION (important fix)
                TempData[BookingKey] = JsonSerializer.Serialize(new BookingSessionDto
                {
                    FlightId = id,
                    ReturnFlightId = returnId,
                    SelectedSeatIds = seatIds,
                    ReturnSeatIds = returnSeatIds,
                    ConflictedSeats = Session?.ConflictedSeats ?? new(),
                    ConflictedSeatsReturn = Session?.ConflictedSeatsReturn ?? new()
                });

                return Page();
            }

            var session = new BookingSessionDto
            {
                FlightId = id,
                ReturnFlightId = returnId,
                SelectedSeatIds = seatIds,
                ReturnSeatIds = returnSeatIds,
                ConflictedSeats = Session?.ConflictedSeats ?? new(),
                ConflictedSeatsReturn = Session?.ConflictedSeatsReturn ?? new()
            };

            TempData[BookingKey] = JsonSerializer.Serialize(session);

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