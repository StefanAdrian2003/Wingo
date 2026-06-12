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

        public List<string> ConflictedSeats { get; set; } = new();
        public List<string> ConflictedLeg2Seats { get; set; } = new();
        public List<string> ConflictedSeatsReturn { get; set; } = new();
        public List<string> ConflictedReturnLeg2Seats { get; set; } = new();

        private string BookingKey =>
              $"Booking:{User.FindFirstValue(ClaimTypes.NameIdentifier)}";

        public Flight? Leg2Flight { get; set; }
        public Flight? ReturnLeg2Flight { get; set; }

        public async Task<IActionResult> OnGetAsync(
                Guid id, Guid? returnId, Guid? leg2Id, Guid? retLeg2Id)
        {
            Flight = await LoadFlight(id);
            if (Flight == null) return NotFound();

            if (leg2Id.HasValue)
                Leg2Flight = await LoadFlight(leg2Id.Value);

            if (returnId.HasValue && returnId.Value != Guid.Empty)
                ReturnFlight = await LoadFlight(returnId.Value);

            if (retLeg2Id.HasValue)
                ReturnLeg2Flight = await LoadFlight(retLeg2Id.Value);

            var sessionJson = TempData.Peek(BookingKey)?.ToString();
            if (!string.IsNullOrEmpty(sessionJson))
                Session = JsonSerializer.Deserialize<BookingSessionDto>(sessionJson);

            if (Session != null)
            {
                ConflictedSeats = Session.ConflictedSeats.ToList();
                ConflictedLeg2Seats = Session.ConflictedLeg2Seats.ToList();
                ConflictedSeatsReturn = Session.ConflictedSeatsReturn.ToList();
                ConflictedReturnLeg2Seats = Session.ConflictedReturnLeg2Seats.ToList();

                Session.ConflictedSeats.Clear();
                Session.ConflictedLeg2Seats.Clear();
                Session.ConflictedSeatsReturn.Clear();
                Session.ConflictedReturnLeg2Seats.Clear();

                TempData[BookingKey] = JsonSerializer.Serialize(Session);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
                Guid id, Guid? returnId, Guid? leg2Id, Guid? retLeg2Id,
                [FromForm] string selectedSeats,
                [FromForm] string? leg2Seats,
                [FromForm] string? returnSeats,
                [FromForm] string? retLeg2Seats)
        {
            var seatIds = JsonSerializer.Deserialize<List<Guid>>(selectedSeats) ?? new();
            var leg2SeatIds = leg2Seats != null ? JsonSerializer.Deserialize<List<Guid>>(leg2Seats) ?? new() : new();
            var returnSeatIds = returnSeats != null ? JsonSerializer.Deserialize<List<Guid>>(returnSeats) ?? new() : new();
            var retLeg2SeatIds = retLeg2Seats != null ? JsonSerializer.Deserialize<List<Guid>>(retLeg2Seats) ?? new() : new();

            bool hasRealReturn = returnId.HasValue && returnId.Value != Guid.Empty;

            if (seatIds.Count == 0)
            {
                ModelState.AddModelError("", "Please select at least one seat.");
                Flight = await LoadFlight(id);
                if (leg2Id.HasValue) Leg2Flight = await LoadFlight(leg2Id.Value);
                if (hasRealReturn) ReturnFlight = await LoadFlight(returnId!.Value);
                if (retLeg2Id.HasValue) ReturnLeg2Flight = await LoadFlight(retLeg2Id.Value);
                return Page();
            }

            // layover outbound: leg1 and leg2 must match
            if (leg2Id.HasValue && leg2SeatIds.Count != seatIds.Count)
            {
                ModelState.AddModelError("",
                    $"Select the same number of seats for both outbound legs. Leg 1: {seatIds.Count}, Leg 2: {leg2SeatIds.Count}.");
                Flight = await LoadFlight(id);
                Leg2Flight = await LoadFlight(leg2Id.Value);
                if (hasRealReturn) ReturnFlight = await LoadFlight(returnId!.Value);
                if (retLeg2Id.HasValue) ReturnLeg2Flight = await LoadFlight(retLeg2Id.Value);
                return Page();
            }

            // direct or layover with return: outbound and return must match
            if (hasRealReturn && returnSeatIds.Count != seatIds.Count)
            {
                ModelState.AddModelError("",
                    $"Select the same number of seats for outbound and return. Outbound: {seatIds.Count}, Return: {returnSeatIds.Count}.");
                Flight = await LoadFlight(id);
                ReturnFlight = await LoadFlight(returnId!.Value);
                if (leg2Id.HasValue) Leg2Flight = await LoadFlight(leg2Id.Value);
                if (retLeg2Id.HasValue) ReturnLeg2Flight = await LoadFlight(retLeg2Id.Value);
                return Page();
            }

            // layover return leg2 must also match
            if (retLeg2Id.HasValue && retLeg2SeatIds.Count != seatIds.Count)
            {
                ModelState.AddModelError("",
                    $"Select the same number of seats for return leg 2. Expected: {seatIds.Count}, Got: {retLeg2SeatIds.Count}.");
                Flight = await LoadFlight(id);
                if (leg2Id.HasValue) Leg2Flight = await LoadFlight(leg2Id.Value);
                if (hasRealReturn) ReturnFlight = await LoadFlight(returnId!.Value);
                ReturnLeg2Flight = await LoadFlight(retLeg2Id.Value);
                return Page();
            }

            var session = new BookingSessionDto
            {
                FlightId = id,
                SelectedSeatIds = seatIds,
                Leg2FlightId = leg2Id,
                Leg2SeatIds = leg2SeatIds,
                ReturnFlightId = returnId,
                ReturnSeatIds = returnSeatIds,
                ReturnLeg2FlightId = retLeg2Id,
                ReturnLeg2SeatIds = retLeg2SeatIds
            };

            TempData[BookingKey] = JsonSerializer.Serialize(session);
            return RedirectToPage("/BookingBaggage");
        }

        private async Task<Flight?> LoadFlight(Guid id)
        {
            return await _context.Flights
                .AsNoTracking()
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