using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System.Text.Json;
using System.Security.Claims;

namespace Proiect_Licenta.Pages
{
    [Authorize]
    public class BookingBaggageModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BookingBaggageModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Flight Flight { get; set; }
        public List<Seat> SelectedSeats { get; set; } = new();

        public Flight? ReturnFlight { get; set; }
        public List<Seat> ReturnSeats { get; set; } = new();

        public BookingSessionDto Session { get; set; }

        private string BookingKey =>
                $"Booking:{User.FindFirstValue(ClaimTypes.NameIdentifier)}";


        public async Task<IActionResult> OnGetAsync()
        {
            var json = TempData.Peek(BookingKey)?.ToString();
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;

            Flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == Session.FlightId);

            if (Flight == null) return NotFound();

            SelectedSeats = await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => Session.SelectedSeatIds.Contains(s.Id))
                .ToListAsync();

            if (Session.IsRoundTrip && Session.ReturnSeatIds.Any())
            {
                ReturnFlight = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == Session.ReturnFlightId);

                ReturnSeats = await _context.Seats
                    .Include(s => s.SeatSection)
                    .Where(s => Session.ReturnSeatIds.Contains(s.Id))
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            [FromForm] List<string> baggageTypes,
            [FromForm] List<bool> hasExtraBags,
            [FromForm] List<string> returnBaggageTypes,
            [FromForm] List<bool> returnHasExtraBags)
        {
            var json = TempData.Peek(BookingKey)?.ToString();
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;

            SelectedSeats = await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => Session.SelectedSeatIds.Contains(s.Id))
                .ToListAsync();

            ReturnSeats = await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => Session.ReturnSeatIds.Contains(s.Id))
                .ToListAsync();

            Session.Baggage = BuildBaggage(SelectedSeats, baggageTypes, hasExtraBags);
            Session.ReturnBaggage = BuildBaggage(ReturnSeats, returnBaggageTypes, returnHasExtraBags);

            TempData[BookingKey] = JsonSerializer.Serialize(Session);
            return RedirectToPage("/BookingReview");
        }

        private List<BaggageSelectionDto> BuildBaggage(
            List<Seat> seats,
            List<string> types,
            List<bool> extras)
        {
            var result = new List<BaggageSelectionDto>();
            for (int i = 0; i < seats.Count; i++)
            {
                var seat = seats[i];
                var baggageType = i < types.Count ? types[i] : "None";
                var hasExtra = i < extras.Count && extras[i];
                decimal price = baggageType switch
                {
                    "Cabin" => 0m,
                    "Checked20" => 25m,
                    "Checked32" => 40m,
                    _ => 0m
                };
                if (hasExtra) price += 35m;

                result.Add(new BaggageSelectionDto
                {
                    SeatId = seat.Id,
                    SeatNumber = seat.SeatNumber,
                    BaggageType = baggageType,
                    HasExtraBag = hasExtra,
                    TotalBaggagePrice = price
                });
            }
            return result;
        }

    }
}
