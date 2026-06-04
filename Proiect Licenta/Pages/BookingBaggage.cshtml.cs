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

        public Flight? Leg2Flight { get; set; }
        public List<Seat> Leg2Seats { get; set; } = new();

        public Flight? ReturnFlight { get; set; }
        public List<Seat> ReturnSeats { get; set; } = new();

        public Flight? ReturnLeg2Flight { get; set; }
        public List<Seat> ReturnLeg2Seats { get; set; } = new();

        public BookingSessionDto Session { get; set; }

        private string BookingKey =>
                $"Booking:{User.FindFirstValue(ClaimTypes.NameIdentifier)}";


        public async Task<IActionResult> OnGetAsync()
        {
            var json = TempData.Peek(BookingKey)?.ToString();
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;

            Flight = await LoadFlight(Session.FlightId);
            if (Flight == null) return NotFound();

            SelectedSeats = await LoadSeats(Session.SelectedSeatIds);

            if (Session.Leg2FlightId.HasValue)
            {
                Leg2Flight = await LoadFlight(Session.Leg2FlightId.Value);
                Leg2Seats = await LoadSeats(Session.Leg2SeatIds);
            }

            bool hasRealReturn = Session.ReturnFlightId.HasValue && Session.ReturnFlightId.Value != Guid.Empty;
            if (hasRealReturn)
            {
                ReturnFlight = await LoadFlight(Session.ReturnFlightId!.Value);
                ReturnSeats = await LoadSeats(Session.ReturnSeatIds);

                if (Session.ReturnLeg2FlightId.HasValue)
                {
                    ReturnLeg2Flight = await LoadFlight(Session.ReturnLeg2FlightId.Value);
                    ReturnLeg2Seats = await LoadSeats(Session.ReturnLeg2SeatIds);
                }
            }

            return Page();
        }


        public async Task<IActionResult> OnPostAsync(
    [FromForm] List<string> baggageTypes,
    [FromForm] List<bool> hasExtraBags,
    [FromForm] List<string> leg2BaggageTypes,
    [FromForm] List<bool> leg2HasExtraBags,
    [FromForm] List<string> returnBaggageTypes,
    [FromForm] List<bool> returnHasExtraBags,
    [FromForm] List<string> retLeg2BaggageTypes,
    [FromForm] List<bool> retLeg2HasExtraBags)
        {
            var json = TempData.Peek(BookingKey)?.ToString();
            if (string.IsNullOrEmpty(json))
                return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;

            SelectedSeats = await LoadSeats(Session.SelectedSeatIds);
            Leg2Seats = await LoadSeats(Session.Leg2SeatIds);
            ReturnSeats = await LoadSeats(Session.ReturnSeatIds);
            ReturnLeg2Seats = await LoadSeats(Session.ReturnLeg2SeatIds);

            Session.Baggage = BuildBaggage(SelectedSeats, baggageTypes, hasExtraBags);
            Session.Leg2Baggage = BuildBaggage(Leg2Seats, leg2BaggageTypes, leg2HasExtraBags);
            Session.ReturnBaggage = BuildBaggage(ReturnSeats, returnBaggageTypes, returnHasExtraBags);
            Session.ReturnLeg2Baggage = BuildBaggage(ReturnLeg2Seats, retLeg2BaggageTypes, retLeg2HasExtraBags);

            TempData[BookingKey] = JsonSerializer.Serialize(Session);
            return RedirectToPage("/BookingReview");
        }

        private async Task<Flight?> LoadFlight(Guid id) =>
            await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == id);

        private async Task<List<Seat>> LoadSeats(List<Guid> ids)
        {
            if (!ids.Any()) return new List<Seat>();
            return await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => ids.Contains(s.Id))
                .ToListAsync();
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
