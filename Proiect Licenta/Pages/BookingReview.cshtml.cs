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


        public BookingSessionDto Session { get; set; }


        public Flight Flight { get; set; }
        public List<Seat> SelectedSeats { get; set; } = new();
        public decimal SeatTotal { get; set; }
        public decimal BaggageTotal { get; set; }


        public Flight? ReturnFlight { get; set; }
        public List<Seat> ReturnSeats { get; set; } = new();
        public decimal ReturnSeatTotal { get; set; }
        public decimal ReturnBaggageTotal { get; set; }


        public Flight? Leg2Flight { get; set; }
        public List<Seat> Leg2Seats { get; set; } = new();
        public decimal Leg2SeatTotal { get; set; }
        public decimal ReturnLeg2SeatTotal { get; set; }


        public Flight? ReturnLeg2Flight { get; set; }
        public List<Seat> ReturnLeg2Seats { get; set; } = new();
        public decimal Leg2BaggageTotal { get; set; }
        public decimal ReturnLeg2BaggageTotal { get; set; }


        public decimal GrandTotal { get; set; }

        private static readonly Dictionary<TravelClass, decimal> Multipliers = new()
        {
            { TravelClass.Economy,  1.0m },
            { TravelClass.Business, 2.2m },
            { TravelClass.First,    3.5m }
        };



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

            var leg2ConflictedSeats = new List<string>();
            var returnLeg2ConflictedSeats = new List<string>();

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


            if (Session.Leg2FlightId.HasValue)
            {
                leg2ConflictedSeats = await _context.FlightSeats
                    .Include(fs => fs.Seat)
                    .Where(fs =>
                        fs.FlightId == Session.Leg2FlightId &&
                        Session.Leg2SeatIds.Contains(fs.SeatId) &&
                        fs.TicketId != null)
                    .Select(fs => fs.Seat.SeatNumber)
                    .ToListAsync();
            }


            if (Session.ReturnLeg2FlightId.HasValue)
            {
                returnLeg2ConflictedSeats = await _context.FlightSeats
                    .Include(fs => fs.Seat)
                    .Where(fs =>
                        fs.FlightId == Session.ReturnLeg2FlightId &&
                        Session.ReturnLeg2SeatIds.Contains(fs.SeatId) &&
                        fs.TicketId != null)
                    .Select(fs => fs.Seat.SeatNumber)
                    .ToListAsync();
            }


            if (conflictedSeats.Any() ||
                leg2ConflictedSeats.Any() ||
                returnConflictedSeats.Any() ||
                returnLeg2ConflictedSeats.Any())
            {
                Session.ConflictedSeats = conflictedSeats;
                Session.ConflictedSeatsReturn = returnConflictedSeats;
                Session.ConflictedLeg2Seats = leg2ConflictedSeats;
                Session.ConflictedReturnLeg2Seats = returnLeg2ConflictedSeats;

                TempData[BookingKey] =
                    JsonSerializer.Serialize(Session);

                return RedirectToPage(
                    "/BookingPassenger",
                    new
                    {
                        id = Session.FlightId,
                        returnId = Session.ReturnFlightId,
                        leg2Id = Session.Leg2FlightId,
                        retLeg2Id = Session.ReturnLeg2FlightId
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

            if (session.Leg2FlightId.HasValue)
            {
                Leg2Flight = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == session.Leg2FlightId);

                Leg2Seats = await _context.Seats
                    .Include(s => s.SeatSection)
                    .Where(s => session.Leg2SeatIds.Contains(s.Id))
                    .ToListAsync();
            }


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


                if (session.ReturnLeg2FlightId.HasValue)
                {
                    ReturnLeg2Flight = await _context.Flights
                        .Include(f => f.Airline)
                        .Include(f => f.DepartureAirport)
                        .Include(f => f.ArrivalAirport)
                        .FirstOrDefaultAsync(f => f.Id == session.ReturnLeg2FlightId);

                    ReturnLeg2Seats = await _context.Seats
                        .Include(s => s.SeatSection)
                        .Where(s => session.ReturnLeg2SeatIds.Contains(s.Id))
                        .ToListAsync();
                }

            }



            SeatTotal = SelectedSeats.Sum(s =>
                Math.Round(
                    Flight!.Price *
                    Multipliers[s.SeatSection.TravelClass], 2));

            Leg2SeatTotal =
                Leg2Flight == null
                    ? 0
                    : Leg2Seats.Sum(s =>
                        Math.Round(
                            Leg2Flight.Price *
                            Multipliers[s.SeatSection.TravelClass], 2));



            ReturnSeatTotal = ReturnFlight == null
                ? 0
                : ReturnSeats.Sum(s =>
                    Math.Round(
                        ReturnFlight.Price *
                        Multipliers[s.SeatSection.TravelClass], 2));

            ReturnLeg2SeatTotal =
                ReturnLeg2Flight == null
                    ? 0
                    : ReturnLeg2Seats.Sum(s =>
                        Math.Round(
                            ReturnLeg2Flight.Price *
                            Multipliers[s.SeatSection.TravelClass], 2));




            BaggageTotal =
                (session.Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m);

            ReturnBaggageTotal =
                (session.ReturnBaggage?.Sum(b => b.TotalBaggagePrice) ?? 0m);

            Leg2BaggageTotal =
                session.Leg2Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;

            ReturnLeg2BaggageTotal =
                session.ReturnLeg2Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;



            GrandTotal =
                SeatTotal +
                Leg2SeatTotal +
                ReturnSeatTotal +
                ReturnLeg2SeatTotal +
                BaggageTotal +
                Leg2BaggageTotal +
                ReturnBaggageTotal +
                ReturnLeg2BaggageTotal;
        }
    }
}
