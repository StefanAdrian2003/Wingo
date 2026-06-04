using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Data
{
    /// <summary>
    /// Seeds a dedicated test scenario for layover + round-trip booking:
    ///
    ///   OUTBOUND (layover):
    ///     Leg 1 — JFK (New York)     → ORD (Chicago)   departs D+1 08:00 UTC, arrives 10:15 UTC  (2h15m)
    ///     Leg 2 — ORD (Chicago)      → LAX (Los Angeles) departs D+1 12:00 UTC, arrives 15:30 UTC (3h30m)
    ///     (1h45m layover at ORD — plenty of time, no overlap)
    ///
    ///   RETURN (layover):
    ///     Leg 1 — LAX (Los Angeles)  → ORD (Chicago)   departs D+5 18:00 UTC, arrives 23:30 UTC  (5h30m)
    ///     Leg 2 — ORD (Chicago)      → JFK (New York)  departs D+6 01:30 UTC, arrives 05:00 UTC  (3h30m)
    ///     (2h layover at ORD overnight — valid connection)
    ///
    /// All 4 flights use the same dedicated test aircraft and airline ("WingoTest").
    /// Run this seeder AFTER AirportSeeder (JFK/ORD/LAX must exist).
    /// </summary>
    public static class TestLayoverSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            const string testEmail = "wingotest@wingo.com";

            // ── Guard: already seeded ────────────────────────────────────────────
            if (await userManager.FindByEmailAsync(testEmail) != null)
            {
                Console.WriteLine("[TestLayoverSeeder] Already seeded — skipping.");
                return;
            }

            // ── 1. Resolve airports ──────────────────────────────────────────────
            var jfk = await context.Airports.FirstOrDefaultAsync(a => a.IATACode == "JFK")
                      ?? throw new InvalidOperationException("Airport JFK not found. Run AirportSeeder first.");
            var ord = await context.Airports.FirstOrDefaultAsync(a => a.IATACode == "ORD")
                      ?? throw new InvalidOperationException("Airport ORD not found. Run AirportSeeder first.");
            var lax = await context.Airports.FirstOrDefaultAsync(a => a.IATACode == "LAX")
                      ?? throw new InvalidOperationException("Airport LAX not found. Run AirportSeeder first.");

            Console.WriteLine($"[TestLayoverSeeder] Using airports: {jfk.Name} / {ord.Name} / {lax.Name}");

            // ── 2. Create test user ──────────────────────────────────────────────
            var user = new User
            {
                FirstName = "WingoTest",
                LastName = "Airlines",
                UserName = testEmail,
                Email = testEmail,
                EmailConfirmed = true,
                IsCompany = true
            };

            var createResult = await userManager.CreateAsync(user, "Company123!");
            if (!createResult.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create test user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(user, "Company");

            // ── 3. Create airline ────────────────────────────────────────────────
            var airline = new Airline
            {
                Name = "WingoTest Airlines",
                IATACode = "WT",
                Country = "United States",
                LogoUrl = "/Logos/WingoTest.png",
                UserId = user.Id,
                User = user
            };
            context.Airlines.Add(airline);
            user.Airline = airline;
            user.AirlineId = airline.Id;

            await context.SaveChangesAsync();

            // ── 4. Create one aircraft (small — easy to fill for testing) ────────
            //   Layout:  First  "AB-CD"     (4/row)  × 2 rows =  8 seats
            //            Business "ABC-DEF" (6/row)  × 3 rows = 18 seats
            //            Economy  "ABC-DEF" (6/row)  × 8 rows = 48 seats
            //            Total = 74 seats

            var aircraft = new Aircraft
            {
                Model = "Boeing 737",
                TotalSeats = 74,
                Airline = airline,
                AirlineId = airline.Id
            };
            context.Aircrafts.Add(aircraft);
            await context.SaveChangesAsync();

            // Helper: create section + its seats
            int currentRow = 1;

            void CreateSection(TravelClass travelClass, string layout, int rows)
            {
                int endRow = currentRow + rows - 1;

                var section = new SeatSection
                {
                    AircraftId = aircraft.Id,
                    TravelClass = travelClass,
                    StartRow = currentRow,
                    EndRow = endRow,
                    Layout = layout
                };
                context.SeatSections.Add(section);

                foreach (int row in Enumerable.Range(currentRow, rows))
                {
                    foreach (var block in layout.Split('-'))
                    {
                        foreach (char letter in block)
                        {
                            context.Seats.Add(new Seat
                            {
                                SeatSectionId = section.Id,
                                TravelClass = travelClass,
                                SeatNumber = $"{row}{letter}"
                            });
                        }
                    }
                }

                currentRow = endRow + 1;
            }

            CreateSection(TravelClass.First, "AB-CD", 2);   //  8 seats, rows 1-2
            CreateSection(TravelClass.Business, "ABC-DEF", 3);   // 18 seats, rows 3-5
            CreateSection(TravelClass.Economy, "ABC-DEF", 8);   // 48 seats, rows 6-13

            await context.SaveChangesAsync();

            // ── 5. Build flight dates ────────────────────────────────────────────
            // All times are UTC.
            // Use "tomorrow" as the base so flights are always in the future.
            var baseDay = DateTime.UtcNow.Date.AddDays(1);

            // OUTBOUND
            // Leg 1: JFK → ORD   departs D+1 08:00, arrives 10:15  (135 min)
            var outLeg1Dep = baseDay.AddHours(8);
            var outLeg1Arr = baseDay.AddHours(10).AddMinutes(15);

            // Leg 2: ORD → LAX   departs D+1 12:00, arrives 15:30  (210 min)
            // *** 1h 45m layover at ORD (10:15 → 12:00) ***
            var outLeg2Dep = baseDay.AddHours(12);
            var outLeg2Arr = baseDay.AddHours(15).AddMinutes(30);

            // RETURN  (4 days after outbound departure)
            var returnBase = baseDay.AddDays(4);

            // Leg 1: LAX → ORD   departs D+5 18:00, arrives 23:30  (330 min)
            var retLeg1Dep = returnBase.AddHours(18);
            var retLeg1Arr = returnBase.AddHours(23).AddMinutes(30);

            // Leg 2: ORD → JFK   departs D+6 01:30, arrives 05:00  (210 min)
            // *** 2h layover at ORD overnight (23:30 → 01:30+1) ***
            var retLeg2Dep = returnBase.AddDays(1).AddHours(1).AddMinutes(30);
            var retLeg2Arr = returnBase.AddDays(1).AddHours(5);

            // ── 6. Add the 4 flights ─────────────────────────────────────────────
            var flightOutLeg1 = new Flight
            {
                FlightNumber = "WT101",
                DepartureAirportId = jfk.Id,
                ArrivalAirportId = ord.Id,
                DepartureTime = outLeg1Dep,
                ArrivalTime = outLeg1Arr,
                DurationMinutes = 135,
                Price = 120m,
                AirlineId = airline.Id,
                AircraftId = aircraft.Id
            };

            var flightOutLeg2 = new Flight
            {
                FlightNumber = "WT102",
                DepartureAirportId = ord.Id,
                ArrivalAirportId = lax.Id,
                DepartureTime = outLeg2Dep,
                ArrivalTime = outLeg2Arr,
                DurationMinutes = 210,
                Price = 150m,
                AirlineId = airline.Id,
                AircraftId = aircraft.Id
            };

            var flightRetLeg1 = new Flight
            {
                FlightNumber = "WT201",
                DepartureAirportId = lax.Id,
                ArrivalAirportId = ord.Id,
                DepartureTime = retLeg1Dep,
                ArrivalTime = retLeg1Arr,
                DurationMinutes = 330,
                Price = 160m,
                AirlineId = airline.Id,
                AircraftId = aircraft.Id
            };

            var flightRetLeg2 = new Flight
            {
                FlightNumber = "WT202",
                DepartureAirportId = ord.Id,
                ArrivalAirportId = jfk.Id,
                DepartureTime = retLeg2Dep,
                ArrivalTime = retLeg2Arr,
                DurationMinutes = 210,
                Price = 130m,
                AirlineId = airline.Id,
                AircraftId = aircraft.Id
            };

            context.Flights.AddRange(flightOutLeg1, flightOutLeg2, flightRetLeg1, flightRetLeg2);
            await context.SaveChangesAsync();

            // ── 7. Create FlightSeat rows for every seat on every flight ─────────
            // (Your booking logic checks FlightSeats to know if a seat is available)
            var allSeats = await context.Seats
                .Where(s => s.SeatSectionId != null &&
                            context.SeatSections
                                .Where(ss => ss.AircraftId == aircraft.Id)
                                .Select(ss => ss.Id)
                                .Contains(s.SeatSectionId))
                .ToListAsync();

            var allFlights = new[] { flightOutLeg1, flightOutLeg2, flightRetLeg1, flightRetLeg2 };

            foreach (var flight in allFlights)
            {
                foreach (var seat in allSeats)
                {
                    context.FlightSeats.Add(new FlightSeat
                    {
                        FlightId = flight.Id,
                        SeatId = seat.Id,
                        TicketId = null   // null = available
                    });
                }
            }

            await context.SaveChangesAsync();

            Console.WriteLine("[TestLayoverSeeder] Done!");
            Console.WriteLine($"  Outbound  Leg 1: WT101  JFK → ORD  {outLeg1Dep:yyyy-MM-dd HH:mm} → {outLeg1Arr:HH:mm} UTC");
            Console.WriteLine($"  Outbound  Leg 2: WT102  ORD → LAX  {outLeg2Dep:yyyy-MM-dd HH:mm} → {outLeg2Arr:HH:mm} UTC");
            Console.WriteLine($"  Return    Leg 1: WT201  LAX → ORD  {retLeg1Dep:yyyy-MM-dd HH:mm} → {retLeg1Arr:HH:mm} UTC");
            Console.WriteLine($"  Return    Leg 2: WT202  ORD → JFK  {retLeg2Dep:yyyy-MM-dd HH:mm} → {retLeg2Arr:HH:mm} UTC");
        }
    }
}