using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;
using System.Text.Json;
using System.Security.Claims;

namespace Proiect_Licenta.Pages
{
    [Authorize]
    public class MockPayModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly BadgeService _badgeService;
        private readonly UserProgressService _progressService;

        public MockPayModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            BadgeService badgeService,
            UserProgressService progressService)
        {
            _context = context;
            _userManager = userManager;
            _badgeService = badgeService;
            _progressService = progressService;
        }


        public BookingSessionDto Session { get; set; }


        public Flight Flight { get; set; }
        public List<Seat> SelectedSeats { get; set; } = new();


        public Flight? ReturnFlight { get; set; }
        public List<Seat> ReturnSeats { get; set; } = new();


        public Flight? Leg2Flight { get; set; }
        public List<Seat> Leg2Seats { get; set; } = new();

        public Flight? ReturnLeg2Flight { get; set; }
        public List<Seat> ReturnLeg2Seats { get; set; } = new();


        public decimal GrandTotal { get; set; }




        [BindProperty]
        public int? SelectedVoucherId { get; set; }
        public List<Voucher> UserVouchers { get; set; } = new();

        public decimal DiscountAmount { get; set; }



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

            var user = await _userManager.GetUserAsync(User);

            UserVouchers = await _context.Vouchers
                .Where(v => v.UserId == user.Id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var json = TempData.Peek(BookingKey)?.ToString();
            if (string.IsNullOrEmpty(json)) return RedirectToPage("/Index");

            Session = JsonSerializer.Deserialize<BookingSessionDto>(json)!;
            await LoadData(Session);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToPage("/Index");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. ── COMPLETE CONFLICT CHECKS FOR ALL 4 LEGS ──
                var takenOutbound = await GetTakenSeatsAsync(Session.FlightId, Session.SelectedSeatIds);
                var takenLeg2 = await GetTakenSeatsAsync(Session.Leg2FlightId, Session.Leg2SeatIds);
                var takenReturn = await GetTakenSeatsAsync(Session.ReturnFlightId, Session.ReturnSeatIds);
                var takenReturnLeg2 = await GetTakenSeatsAsync(Session.ReturnLeg2FlightId, Session.ReturnLeg2SeatIds);

                if (takenOutbound.Any() || takenLeg2.Any() || takenReturn.Any() || takenReturnLeg2.Any())
                {
                    await transaction.RollbackAsync();

                    Session.ConflictedSeats = await GetSeatNumbersAsync(takenOutbound);
                    Session.ConflictedLeg2Seats = await GetSeatNumbersAsync(takenLeg2);
                    Session.ConflictedSeatsReturn = await GetSeatNumbersAsync(takenReturn);
                    Session.ConflictedReturnLeg2Seats = await GetSeatNumbersAsync(takenReturnLeg2);

                    TempData[BookingKey] = JsonSerializer.Serialize(Session);
                    return RedirectToPage("/BookingPassenger", new { id = Session.FlightId, returnId = Session.ReturnFlightId });
                }

                // Calculate voucher discount factors
                decimal discountMultiplier = 0m;
                Voucher? voucher = null;
                if (SelectedVoucherId.HasValue)
                {
                    voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Id == SelectedVoucherId.Value && v.UserId == currentUser.Id);
                    if (voucher != null) discountMultiplier = voucher.DiscountPercent / 100m;
                }

                // 2. ── PROCESS BOOKINGS USING A SECURE DICTIONARY MAP (Fixes Bug 1) ──
                // Load seats into dictionaries to guarantee O(1) matching based precisely on DTO index positions
                var outboundSeatsMap = SelectedSeats.ToDictionary(s => s.Id);

                // Process Outbound Leg 1
                await ProcessLegBookingAsync(bookingUser: currentUser,
                                             flightId: Session.FlightId,
                                             baseFlightPrice: Flight.Price,
                                             orderedSeatIds: Session.SelectedSeatIds,
                                             seatsMap: outboundSeatsMap,
                                             baggageSelections: Session.Baggage,
                                             discountMultiplier: discountMultiplier);

                // Process Outbound Leg 2 (Layover)
                if (Session.Leg2FlightId.HasValue && Leg2Seats.Any())
                {
                    var leg2Map = Leg2Seats.ToDictionary(s => s.Id);
                    await ProcessLegBookingAsync(currentUser, Session.Leg2FlightId.Value, Leg2Flight!.Price, Session.Leg2SeatIds, leg2Map, Session.Leg2Baggage, discountMultiplier);
                }

                // Process Return Leg 1
                if (Session.IsRoundTrip && ReturnSeats.Any())
                {
                    var returnMap = ReturnSeats.ToDictionary(s => s.Id);
                    await ProcessLegBookingAsync(currentUser, Session.ReturnFlightId!.Value, ReturnFlight!.Price, Session.ReturnSeatIds, returnMap, Session.ReturnBaggage, discountMultiplier);
                }

                // Process Return Leg 2 (Round Trip Connection Layover)
                if (Session.ReturnLeg2FlightId.HasValue && ReturnLeg2Seats.Any())
                {
                    var returnLeg2Map = ReturnLeg2Seats.ToDictionary(s => s.Id);
                    await ProcessLegBookingAsync(currentUser, Session.ReturnLeg2FlightId.Value, ReturnLeg2Flight!.Price, Session.ReturnLeg2SeatIds, returnLeg2Map, Session.ReturnLeg2Baggage, discountMultiplier);
                }

                // 3. ── UPDATE USER STATISTICS & CLEANUP ──
                int totalLegsBooked = (SelectedSeats.Any() ? 1 : 0) + (Leg2Seats.Any() ? 1 : 0) + (ReturnSeats.Any() ? 1 : 0) + (ReturnLeg2Seats.Any() ? 1 : 0);
                currentUser.FlightsBooked += totalLegsBooked;
                await _userManager.UpdateAsync(currentUser);
                await _progressService.AddPointsAsync(currentUser, 50 * SelectedSeats.Count);

                if (voucher != null) _context.Vouchers.Remove(voucher);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData.Remove(BookingKey);
                TempData["StatusMessage"] = $"Booking confirmed! {SelectedSeats.Count} ticket(s) booked.";
                return RedirectToPage("/Account/Manage/Bookings", new { area = "Identity" });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return Page();
            }
        }




        private async Task<List<Guid>> GetTakenSeatsAsync(Guid? flightId, List<Guid> seatIds)
        {
            if (!flightId.HasValue || seatIds == null || !seatIds.Any()) return new List<Guid>();

            return await _context.FlightSeats
                .Where(fs => fs.FlightId == flightId.Value && seatIds.Contains(fs.SeatId) && fs.TicketId != null)
                .Select(fs => fs.SeatId)
                .ToListAsync();
        }

        private async Task<List<string>> GetSeatNumbersAsync(List<Guid> seatIds)
        {
            if (!seatIds.Any()) return new List<string>();
            return await _context.Seats.Where(s => seatIds.Contains(s.Id)).Select(s => s.SeatNumber).ToListAsync();
        }

        private async Task ProcessLegBookingAsync(User bookingUser, Guid flightId, decimal baseFlightPrice, List<Guid> orderedSeatIds, Dictionary<Guid, Seat> seatsMap, List<BaggageSelectionDto> baggageSelections, decimal discountMultiplier)
        {
            var booking = new Booking { UserId = bookingUser.Id, FlightId = flightId, BookingDate = DateTime.UtcNow };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(); // Generates Booking ID

            for (int i = 0; i < orderedSeatIds.Count; i++)
            {
                var seatId = orderedSeatIds[i];
                if (!seatsMap.TryGetValue(seatId, out var seat)) continue;

                var travelClass = seat.SeatSection.TravelClass;
                var seatPrice = Math.Round(baseFlightPrice * Multipliers[travelClass], 2);
                var baggageDto = baggageSelections?.ElementAtOrDefault(i);
                var originalPrice = seatPrice + (baggageDto?.TotalBaggagePrice ?? 0m);

                var ticket = new Ticket
                {
                    BookingId = booking.Id,
                    TravelClass = travelClass,
                    Price = Math.Round(originalPrice * (1 - discountMultiplier), 2)
                };
                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync(); // Generates Ticket ID

                // FIXES BUG 2: Updates inventory record if it exists, otherwise creates it safely
                var existingFlightSeat = await _context.FlightSeats.FirstOrDefaultAsync(fs => fs.FlightId == flightId && fs.SeatId == seatId);
                if (existingFlightSeat != null)
                {
                    existingFlightSeat.TicketId = ticket.Id;
                }
                else
                {
                    _context.FlightSeats.Add(new FlightSeat { FlightId = flightId, SeatId = seatId, TicketId = ticket.Id });
                }

                // Save Baggage details
                if (baggageDto != null && baggageDto.BaggageType != "None")
                {
                    var (baggageType, weight, price) = baggageDto.BaggageType switch
                    {
                        "Cabin" => (BaggageType.Cabin, 8, 0m),
                        "Checked20" => (BaggageType.Checked, 20, 25m),
                        "Checked32" => (BaggageType.Extra, 32, 40m),
                        _ => (BaggageType.Cabin, 8, 0m)
                    };

                    _context.baggageItems.Add(new BaggageItem { TicketId = ticket.Id, Type = baggageType, WeightKg = weight, Price = price });
                }

                if (baggageDto?.HasExtraBag == true)
                {
                    _context.baggageItems.Add(new BaggageItem { TicketId = ticket.Id, Type = BaggageType.Extra, WeightKg = 23, Price = 35m });
                }
            }
        }




        private async Task LoadData(BookingSessionDto session)
        {
            // ─────────────────────────────────────────
            // FLIGHTS
            // ─────────────────────────────────────────

            Flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == session.FlightId);

            if (session.Leg2FlightId.HasValue)
            {
                Leg2Flight = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == session.Leg2FlightId);
            }

            if (session.ReturnFlightId.HasValue)
            {
                ReturnFlight = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == session.ReturnFlightId);
            }

            if (session.ReturnLeg2FlightId.HasValue)
            {
                ReturnLeg2Flight = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == session.ReturnLeg2FlightId);
            }

            // ─────────────────────────────────────────
            // SEATS (ALL LEGS)
            // ─────────────────────────────────────────

            SelectedSeats = await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => session.SelectedSeatIds.Contains(s.Id))
                .ToListAsync();

            if (session.Leg2SeatIds.Any())
            {
                Leg2Seats = await _context.Seats
                    .Include(s => s.SeatSection)
                    .Where(s => session.Leg2SeatIds.Contains(s.Id))
                    .ToListAsync();
            }

            ReturnSeats = await _context.Seats
                .Include(s => s.SeatSection)
                .Where(s => session.ReturnSeatIds.Contains(s.Id))
                .ToListAsync();

            if (session.ReturnLeg2SeatIds.Any())
            {
                ReturnLeg2Seats = await _context.Seats
                    .Include(s => s.SeatSection)
                    .Where(s => session.ReturnLeg2SeatIds.Contains(s.Id))
                    .ToListAsync();
            }

            // ─────────────────────────────────────────
            // PRICING
            // ─────────────────────────────────────────

            decimal seatTotal =
                SelectedSeats.Sum(s =>
                    Math.Round(Flight.Price * Multipliers[s.SeatSection.TravelClass], 2));

            decimal leg2SeatTotal =
                Leg2Flight == null ? 0 :
                Leg2Seats.Sum(s =>
                    Math.Round(Leg2Flight.Price * Multipliers[s.SeatSection.TravelClass], 2));

            decimal returnSeatTotal =
                ReturnFlight == null ? 0 :
                ReturnSeats.Sum(s =>
                    Math.Round(ReturnFlight.Price * Multipliers[s.SeatSection.TravelClass], 2));

            decimal returnLeg2SeatTotal =
                ReturnLeg2Flight == null ? 0 :
                ReturnLeg2Seats.Sum(s =>
                    Math.Round(ReturnLeg2Flight.Price * Multipliers[s.SeatSection.TravelClass], 2));

            // ─────────────────────────────────────────
            // BAGGAGE
            // ─────────────────────────────────────────

            decimal baggageTotal =
                session.Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;

            decimal leg2BaggageTotal =
                session.Leg2Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;

            decimal returnBaggageTotal =
                session.ReturnBaggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;

            decimal returnLeg2BaggageTotal =
                session.ReturnLeg2Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;

            // ─────────────────────────────────────────
            // FINAL TOTAL
            // ─────────────────────────────────────────

            GrandTotal =
                seatTotal +
                leg2SeatTotal +
                returnSeatTotal +
                returnLeg2SeatTotal +
                baggageTotal +
                leg2BaggageTotal +
                returnBaggageTotal +
                returnLeg2BaggageTotal;
        }
    }
}