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
    public class BookingBaggageModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BookingBaggageModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Flight Flight { get; set; }
        public List<Seat> SelectedSeats { get; set; } = new();
        public BookingSessionDto Session { get; set; }

        // Baggage prices (euros)
        public static decimal CabinPrice = 0m;
        public static decimal Checked20Price = 25m;
        public static decimal Checked32Price = 40m;
        public static decimal ExtraPrice = 35m;

        public async Task<IActionResult> OnGetAsync()
        {
            var json = TempData.Peek("BookingSession") as string;
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            [FromForm] List<string> baggageTypes,
            [FromForm] List<bool> hasExtraBags)
        {
            var json = TempData.Peek("BookingSession") as string;
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;

            SelectedSeats = await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => Session.SelectedSeatIds.Contains(s.Id))
                .ToListAsync();

            // build baggage selections
            Session.Baggage = new List<BaggageSelectionDto>();

            for (int i = 0; i < SelectedSeats.Count; i++)
            {
                var seat = SelectedSeats[i];
                var baggageType = i < baggageTypes.Count ? baggageTypes[i] : "None";
                var hasExtra = i < hasExtraBags.Count && hasExtraBags[i];

                decimal baggagePrice = baggageType switch
                {
                    "Cabin" => CabinPrice,
                    "Checked20" => Checked20Price,
                    "Checked32" => Checked32Price,
                    _ => 0m
                };

                if (hasExtra) baggagePrice += ExtraPrice;

                Session.Baggage.Add(new BaggageSelectionDto
                {
                    SeatId = seat.Id,
                    SeatNumber = seat.SeatNumber,
                    BaggageType = baggageType,
                    HasExtraBag = hasExtra,
                    TotalBaggagePrice = baggagePrice
                });
            }

            TempData["BookingSession"] = JsonSerializer.Serialize(Session);
            return RedirectToPage("/BookingReview");
        }
    }
}
