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


        public decimal SeatTotal { get; set; }
        public decimal BaggageTotal { get; set; }
        public decimal GrandTotal { get; set; }

        private static readonly Dictionary<TravelClass, decimal> Multipliers = new()
        {
            { TravelClass.Economy,  1.0m },
            { TravelClass.Business, 2.2m },
            { TravelClass.First,    3.5m }
        };



        public Flight? ReturnFlight { get; set; }
        public List<Seat> ReturnSeats { get; set; } = new();

        public decimal ReturnSeatTotal { get; set; }
        public decimal ReturnBaggageTotal { get; set; }

        private string BookingKey =>
             $"Booking:{User.FindFirstValue(ClaimTypes.NameIdentifier)}";

        public async Task<IActionResult> OnGetAsync()
        {
            var json = TempData.Peek(BookingKey)?.ToString();
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;
            await LoadData(Session);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var json = TempData.Peek(BookingKey)?.ToString();
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

            var returnConflictedSeats = new List<string>();

            if (Session.IsRoundTrip)
            {
                returnConflictedSeats = await _context.FlightSeats
                    .Include(fs => fs.Seat)
                    .Where(fs =>
                        fs.FlightId == Session.ReturnFlightId &&
                        Session.ReturnSeatIds.Contains(fs.SeatId) &&
                        fs.TicketId != null)
                    .Select(fs => fs.Seat.SeatNumber)
                    .ToListAsync();
            }

            if (conflictedSeats.Any() || returnConflictedSeats.Any())
            {
                Session.ConflictedSeats = conflictedSeats;
                Session.ConflictedSeatsReturn = returnConflictedSeats;

                TempData[BookingKey] =
                    JsonSerializer.Serialize(Session);

                return RedirectToPage(
                    "/BookingPassenger",
                    new
                    {
                        id = Session.FlightId,
                        returnId = Session.ReturnFlightId
                    });
            }

            // all seats still available — proceed to mock pay
            TempData[BookingKey] = json; // keep session alive
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


            if (session.IsRoundTrip)
            {
                ReturnFlight = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == session.ReturnFlightId);

                ReturnSeats = await _context.Seats
                    .Include(s => s.SeatSection)
                    .Where(s => session.ReturnSeatIds.Contains(s.Id))
                    .ToListAsync();
            }

            SeatTotal = SelectedSeats.Sum(s =>
                Math.Round(
                    Flight!.Price *
                    Multipliers[s.SeatSection.TravelClass], 2));

            ReturnSeatTotal = ReturnFlight == null
                ? 0
                : ReturnSeats.Sum(s =>
                    Math.Round(
                        ReturnFlight.Price *
                        Multipliers[s.SeatSection.TravelClass], 2));

            BaggageTotal =
                (session.Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m);

            ReturnBaggageTotal =
                (session.ReturnBaggage?.Sum(b => b.TotalBaggagePrice) ?? 0m);



            GrandTotal =
                SeatTotal +
                ReturnSeatTotal +
                BaggageTotal +
                ReturnBaggageTotal;
        }
    }
}
