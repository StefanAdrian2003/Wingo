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

        public Flight Flight { get; set; }
        public List<Seat> SelectedSeats { get; set; } = new();
        public BookingSessionDto Session { get; set; }
        public decimal GrandTotal { get; set; }

        public Flight? ReturnFlight { get; set; }
        public List<Seat> ReturnSeats { get; set; } = new();

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
            await LoadData(Session);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Index");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ── OUTBOUND CONFLICTS ──
                var takenOutboundSeatIds = await _context.FlightSeats
                    .Where(fs =>
                        fs.FlightId == Session.FlightId &&
                        Session.SelectedSeatIds.Contains(fs.SeatId) &&
                        fs.TicketId != null)
                    .Select(fs => fs.SeatId)
                    .ToListAsync();

                // ── RETURN CONFLICTS ──
                var takenReturnSeatIds = new List<Guid>();

                if (Session.IsRoundTrip)
                {
                    takenReturnSeatIds = await _context.FlightSeats
                        .Where(fs =>
                            fs.FlightId == Session.ReturnFlightId &&
                            Session.ReturnSeatIds.Contains(fs.SeatId) &&
                            fs.TicketId != null)
                        .Select(fs => fs.SeatId)
                        .ToListAsync();
                }

                if (takenOutboundSeatIds.Any() || takenReturnSeatIds.Any())
                {
                    await transaction.RollbackAsync();

                    Session.ConflictedSeats = await _context.Seats
                        .Where(s => takenOutboundSeatIds.Contains(s.Id))
                        .Select(s => s.SeatNumber)
                        .ToListAsync();

                    Session.ConflictedSeatsReturn = await _context.Seats
                        .Where(s => takenReturnSeatIds.Contains(s.Id))
                        .Select(s => s.SeatNumber)
                        .ToListAsync();

                    TempData[BookingKey] = JsonSerializer.Serialize(Session);

                    return RedirectToPage("/BookingPassenger", new
                    {
                        id = Session.FlightId,
                        returnId = Session.ReturnFlightId
                    });
                }

                // ── CREATE BOOKING ──
                var booking = new Booking
                {
                    UserId = currentUser.Id,
                    FlightId = Session.FlightId,
                    BookingDate = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                for (int i = 0; i < SelectedSeats.Count; i++)
                {
                    var seat = SelectedSeats[i];
                    var travelClass = seat.SeatSection.TravelClass;

                    var seatPrice = Math.Round(
                        Flight.Price * Multipliers[travelClass], 2);

                    var baggage = Session.Baggage?.ElementAtOrDefault(i);

                    var ticket = new Ticket
                    {
                        BookingId = booking.Id,
                        TravelClass = travelClass,
                        Price = seatPrice + (baggage?.TotalBaggagePrice ?? 0m)
                    };

                    _context.Tickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    _context.FlightSeats.Add(new FlightSeat
                    {
                        FlightId = Session.FlightId,
                        SeatId = seat.Id,
                        TicketId = ticket.Id
                    });

                    if (baggage != null && baggage.BaggageType != "None")
                    {
                        var baggageType = baggage.BaggageType switch
                        {
                            "Cabin" => BaggageType.Cabin,
                            "Checked20" => BaggageType.Checked,
                            "Checked32" => BaggageType.Extra,
                            _ => BaggageType.Cabin
                        };

                        _context.baggageItems.Add(new BaggageItem
                        {
                            TicketId = ticket.Id,
                            Type = baggageType,
                            WeightKg = baggage.BaggageType == "Checked20" ? 20 : 32,
                            Price = baggage.BaggageType == "Cabin" ? 0m :
                                    baggage.BaggageType == "Checked20" ? 25m : 40m
                        });
                    }

                    if (baggage?.HasExtraBag == true)
                    {
                        _context.baggageItems.Add(new BaggageItem
                        {
                            TicketId = ticket.Id,
                            Type = BaggageType.Extra,
                            WeightKg = 23,
                            Price = 35m
                        });
                    }
                }








                // ── CREATE RETURN BOOKING (ONLY IF ROUND TRIP) ──
                if (Session.IsRoundTrip)
                {
                    var bookingReturn = new Booking
                    {
                        UserId = currentUser.Id,
                        FlightId = Session.ReturnFlightId!.Value,
                        BookingDate = DateTime.UtcNow
                    };

                    _context.Bookings.Add(bookingReturn);
                    await _context.SaveChangesAsync();

                    for (int i = 0; i < ReturnSeats.Count; i++)
                    {
                        var seat = ReturnSeats[i];
                        var travelClass = seat.SeatSection.TravelClass;

                        var seatPrice = Math.Round(
                            ReturnFlight!.Price * Multipliers[travelClass], 2);

                        var baggage = Session.ReturnBaggage?.ElementAtOrDefault(i);

                        var ticket = new Ticket
                        {
                            BookingId = bookingReturn.Id,
                            TravelClass = travelClass,
                            Price = seatPrice + (baggage?.TotalBaggagePrice ?? 0m)
                        };

                        _context.Tickets.Add(ticket);
                        await _context.SaveChangesAsync();

                        _context.FlightSeats.Add(new FlightSeat
                        {
                            FlightId = Session.ReturnFlightId.Value,
                            SeatId = seat.Id,
                            TicketId = ticket.Id
                        });

                        if (baggage != null && baggage.BaggageType != "None")
                        {
                            var baggageType = baggage.BaggageType switch
                            {
                                "Cabin" => BaggageType.Cabin,
                                "Checked20" => BaggageType.Checked,
                                "Checked32" => BaggageType.Extra,
                                _ => BaggageType.Cabin
                            };

                            _context.baggageItems.Add(new BaggageItem
                            {
                                TicketId = ticket.Id,
                                Type = baggageType,
                                WeightKg = baggage.BaggageType == "Checked20" ? 20 : 32,
                                Price = baggage.BaggageType == "Cabin" ? 0m :
                                        baggage.BaggageType == "Checked20" ? 25m : 40m
                            });
                        }

                        if (baggage?.HasExtraBag == true)
                        {
                            _context.baggageItems.Add(new BaggageItem
                            {
                                TicketId = ticket.Id,
                                Type = BaggageType.Extra,
                                WeightKg = 23,
                                Price = 35m
                            });
                        }
                    }
                }


                await _context.SaveChangesAsync();

                currentUser.FlightsBooked++;

                if (Session.IsRoundTrip)
                {
                    currentUser.FlightsBooked++;
                }

                await _userManager.UpdateAsync(currentUser);

                await _progressService.AddPointsAsync(currentUser, 50 * SelectedSeats.Count);

                await transaction.CommitAsync();

                TempData.Remove(BookingKey);

                TempData["StatusMessage"] =
                    $"Booking confirmed! {SelectedSeats.Count} ticket(s) booked.";

                return RedirectToPage("/Account/Manage/Bookings",
                    new { area = "Identity" });
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return Page();
            }
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

            var seatTotal = SelectedSeats.Sum(s =>
                Math.Round(Flight.Price * Multipliers[s.SeatSection.TravelClass], 2));

            var returnSeatTotal = ReturnFlight == null ? 0 :
                ReturnSeats.Sum(s =>
                    Math.Round(ReturnFlight.Price * Multipliers[s.SeatSection.TravelClass], 2));

            var baggageTotal = Session.Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;
            var returnBaggageTotal = Session.ReturnBaggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;

            GrandTotal = seatTotal + returnSeatTotal + baggageTotal + returnBaggageTotal;
        }
    }
}