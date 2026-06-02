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
    public class BookingReviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BookingReviewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Flight Flight { get; set; }
        public List<Seat> SelectedSeats { get; set; } = new();
        public BookingSessionDto Session { get; set; }

        public static readonly decimal CabinPrice = 0m;
        public static readonly decimal Checked20Price = 25m;
        public static readonly decimal Checked32Price = 40m;
        public static readonly decimal ExtraPrice = 35m;

        public decimal SeatTotal { get; set; }
        public decimal BaggageTotal { get; set; }
        public decimal GrandTotal { get; set; }

        private static readonly Dictionary<TravelClass, decimal> Multipliers = new()
        {
            { TravelClass.Economy,  1.0m },
            { TravelClass.Business, 2.2m },
            { TravelClass.First,    3.5m }
        };

        public async Task<IActionResult> OnGetAsync()
        {
            var json = TempData.Peek("BookingSession") as string;
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;
            await LoadData(Session);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var json = TempData.Peek("BookingSession") as string;
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;

            // ── AVAILABILITY RE-CHECK ──
            var conflictedSeats = await _context.FlightSeats
                .Include(fs => fs.Seat)
                .Where(fs =>
                    fs.FlightId == Session.FlightId &&
                    Session.SelectedSeatIds.Contains(fs.SeatId) &&
                    fs.TicketId != null)
                .Select(fs => fs.Seat.SeatNumber)
                .ToListAsync();

            if (conflictedSeats.Any())
            {
                // seats were taken — send user back to seat picker with warning
                TempData["ConflictedSeatsJson"] = JsonSerializer.Serialize(conflictedSeats);
                TempData.Remove("BookingSession");
                return RedirectToPage("/BookingPassenger", new { id = Session.FlightId });
            }

            // all seats still available — proceed to mock pay
            TempData["BookingSession"] = json; // keep session alive
            return RedirectToPage("/MockPay");
        }

        private async Task LoadData(BookingSessionDto session)
        {
            Flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == session.FlightId);

            SelectedSeats = await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => session.SelectedSeatIds.Contains(s.Id))
                .ToListAsync();

            SeatTotal = SelectedSeats.Sum(s =>
                Math.Round(Flight!.Price * Multipliers[s.SeatSection.TravelClass], 2));

            BaggageTotal = session.Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;
            GrandTotal = SeatTotal + BaggageTotal;
        }
    }
}
