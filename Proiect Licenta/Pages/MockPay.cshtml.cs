using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;
using System.Text.Json;

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
            await LoadData(Session);

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToPage("/Index");

            // ── FINAL AVAILABILITY CHECK inside a transaction ──
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // check once more — race condition guard
                var takenSeatIds = await _context.FlightSeats
                    .Where(fs =>
                        fs.FlightId == Session.FlightId &&
                        Session.SelectedSeatIds.Contains(fs.SeatId) &&
                        fs.TicketId != null)
                    .Select(fs => fs.SeatId)
                    .ToListAsync();

                if (takenSeatIds.Any())
                {
                    await transaction.RollbackAsync();
                    var takenNumbers = await _context.Seats
                        .Where(s => takenSeatIds.Contains(s.Id))
                        .Select(s => s.SeatNumber)
                        .ToListAsync();

                    TempData["ConflictedSeatsJson"] = JsonSerializer.Serialize(takenNumbers);
                    TempData.Remove("BookingSession");
                    return RedirectToPage("/BookingPassenger", new { id = Session.FlightId });
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

                // ── CREATE TICKETS + FLIGHTSEATS + BAGGAGE ──
                for (int i = 0; i < SelectedSeats.Count; i++)
                {
                    var seat = SelectedSeats[i];
                    var travelClass = seat.SeatSection.TravelClass;
                    var seatPrice = Math.Round(Flight.Price * Multipliers[travelClass], 2);
                    var baggage = Session.Baggage?.ElementAtOrDefault(i);

                    // ticket
                    var ticket = new Ticket
                    {
                        BookingId = booking.Id,
                        TravelClass = travelClass,
                        Price = seatPrice + (baggage?.TotalBaggagePrice ?? 0m)
                    };
                    _context.Tickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    // flightseat — marks seat as taken
                    var flightSeat = new FlightSeat
                    {
                        FlightId = Session.FlightId,
                        SeatId = seat.Id,
                        TicketId = ticket.Id
                    };
                    _context.FlightSeats.Add(flightSeat);

                    // baggage items
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

                await _context.SaveChangesAsync();

                // ── UPDATE USER STATS ──
                currentUser.FlightsBooked++;
                await _userManager.UpdateAsync(currentUser);

                // ── AWARD POINTS + BADGES ──
                await _progressService.AddPointsAsync(currentUser, 50 * SelectedSeats.Count);
                //await _badgeService.AwardBadgeByNameAsync(currentUser.Id, "");

                await transaction.CommitAsync();
                TempData.Remove("BookingSession");

                TempData["StatusMessage"] = $"Booking confirmed! {SelectedSeats.Count} ticket(s) booked.";
                return RedirectToPage("/Account/Manage/Bookings",
                    new { area = "Identity" });
            }
            catch (Exception)
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

            var seatTotal = SelectedSeats.Sum(s =>
                Math.Round(Flight!.Price * Multipliers[s.SeatSection.TravelClass], 2));
            var baggageTotal = session.Baggage?.Sum(b => b.TotalBaggagePrice) ?? 0m;
            GrandTotal = seatTotal + baggageTotal;
        }
    }
}
