using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages.Flights
{
    [Authorize(Roles = "Company")]
    [EnableRateLimiting("fixed")]
    public class AddFlightModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AddFlightModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty] public Guid DepartureAirportId { get; set; }
        [BindProperty] public Guid ArrivalAirportId { get; set; }
        [BindProperty] public DateTime DepartureTime { get; set; }
        [BindProperty] public int DurationMinutes { get; set; }

        [BindProperty] public CreateFlight Input { get; set; } = new();
        public string SelectedDepartureName { get; set; }
        public string SelectedArrivalName { get; set; }

        public List<Aircraft> Aircrafts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Aircrafts = new List<Aircraft>();
            return Page();
        }



        public async Task<IActionResult> OnPostCheckAircraftAsync()
        {
            var arrival = DepartureTime.AddMinutes(DurationMinutes);

            await LoadSelectedAirportNames();

            var user = await GetUserAsync();

            var allAircrafts = await _context.Aircrafts
                .Where(a => a.AirlineId == user.AirlineId.Value)
                .Include(a => a.Flights)
                .ToListAsync();

            Aircrafts = new List<Aircraft>();

            foreach (var aircraft in allAircrafts)
            {
                var flights = aircraft.Flights
                    .OrderBy(f => f.DepartureTime)
                    .ToList();

                var previousFlight = flights
                    .Where(f => f.ArrivalTime <= DepartureTime)
                    .OrderByDescending(f => f.ArrivalTime)
                    .FirstOrDefault();

                var nextFlight = flights
                    .Where(f => f.DepartureTime >= DepartureTime)
                    .OrderBy(f => f.DepartureTime)
                    .FirstOrDefault();

                bool valid = true;

                // ---------- CHECK PREVIOUS ----------
                if (previousFlight != null)
                {
                    var sameAirport =
                        previousFlight.ArrivalAirportId == DepartureAirportId;

                    var buffer = sameAirport
                        ? TimeSpan.FromHours(1)
                        : TimeSpan.FromHours(5);

                    if (DepartureTime < previousFlight.ArrivalTime.Add(buffer))
                    {
                        valid = false;
                    }
                }

                // ---------- CHECK NEXT ----------
                if (nextFlight != null)
                {
                    var sameAirport =
                        nextFlight.DepartureAirportId == ArrivalAirportId;

                    var buffer = sameAirport
                        ? TimeSpan.FromHours(1)
                        : TimeSpan.FromHours(5);

                    if (arrival > nextFlight.DepartureTime.Subtract(buffer))
                    {
                        valid = false;
                    }
                }

                if (valid)
                {
                    Aircrafts.Add(aircraft);
                }
            }


            if (!Aircrafts.Any())
            {
                ModelState.AddModelError("",
                    $"No aircraft available between {DepartureTime:hh:mm tt} and {arrival:hh:mm tt}.");
            }


            return Page();
        }



        public async Task<IActionResult> OnPostCreateFlightAsync()
        {
            var user = await GetUserAsync();
            await LoadSelectedAirportNames();
            if (user == null)
                return Unauthorized();

            // ---------------- VALIDĂRI BASIC ----------------
            if (DepartureAirportId == Guid.Empty)
                ModelState.AddModelError("", "Departure airport required");

            if (ArrivalAirportId == Guid.Empty)
                ModelState.AddModelError("", "Arrival airport required");

            if (DepartureAirportId == ArrivalAirportId)
                ModelState.AddModelError("", "Airports must be different");

            if (DepartureTime < DateTime.Now)
                ModelState.AddModelError("", "Invalid departure time");

            if (DurationMinutes <= 0)
                ModelState.AddModelError("", "Invalid duration");

            if (string.IsNullOrWhiteSpace(Input.FlightNumber))
                ModelState.AddModelError("", "Flight number required");

            if (Input.Price <= 0)
                ModelState.AddModelError("", "Invalid price");

            if (Input.AircraftId == Guid.Empty)
                ModelState.AddModelError("", "Select aircraft");

            var arrival = DepartureTime.AddMinutes(DurationMinutes);

            // ---------------- RECALCULARE AVIOANE DISPONIBILE ----------------
            var allAircrafts = await _context.Aircrafts
                .Include(a => a.Flights)
                .ToListAsync();

            Aircrafts = new List<Aircraft>();

            foreach (var aircraft in allAircrafts)
            {
                var flightsForAircraft = aircraft.Flights
                    .OrderBy(f => f.DepartureTime)
                    .ToList();

                var previousFlight = flightsForAircraft
                    .Where(f => f.ArrivalTime <= DepartureTime)
                    .OrderByDescending(f => f.ArrivalTime)
                    .FirstOrDefault();

                var nextFlight = flightsForAircraft
                    .Where(f => f.DepartureTime >= DepartureTime)
                    .OrderBy(f => f.DepartureTime)
                    .FirstOrDefault();

                bool valid = true;

                // ---------- CHECK PREVIOUS ----------
                if (previousFlight != null)
                {
                    var sameAirport =
                        previousFlight.ArrivalAirportId == DepartureAirportId;

                    var buffer = sameAirport
                        ? TimeSpan.FromHours(1)
                        : TimeSpan.FromHours(5);

                    if (DepartureTime < previousFlight.ArrivalTime.Add(buffer))
                    {
                        valid = false;
                    }
                }

                // ---------- CHECK NEXT ----------
                if (nextFlight != null)
                {
                    var sameAirport =
                        nextFlight.DepartureAirportId == ArrivalAirportId;

                    var buffer = sameAirport
                        ? TimeSpan.FromHours(1)
                        : TimeSpan.FromHours(5);

                    if (arrival > nextFlight.DepartureTime.Subtract(buffer))
                    {
                        valid = false;
                    }
                }

                if (valid)
                {
                    Aircrafts.Add(aircraft);
                }
            }


            // ---------------- VALIDĂRI FINALE ----------------
            if (!ModelState.IsValid)
                return Page();

            // avionul selectat nu mai este disponibil
            if (!Aircrafts.Any(a => a.Id == Input.AircraftId))
            {
                ModelState.AddModelError("",
                    "Selected aircraft is no longer available.");

                return Page();
            }

            // ---------------- CREARE ZBOR ----------------
            var flight = new Flight
            {
                FlightNumber = Input.FlightNumber,
                Price = Input.Price,
                AircraftId = Input.AircraftId,
                DepartureTime = DepartureTime,
                ArrivalTime = arrival,
                DurationMinutes = DurationMinutes,
                DepartureAirportId = DepartureAirportId,
                ArrivalAirportId = ArrivalAirportId,
                AirlineId = user.AirlineId.Value
            };

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<JsonResult> OnGetAirportsAsync(string term)
        {
            var result = await _context.Airports
                .Where(a => a.Name.Contains(term) ||
                            a.IATACode.Contains(term) ||
                            a.City.Contains(term))
                .Take(10)
                .Select(a => new
                {
                    id = a.Id,
                    text = $"{a.Name} - {a.City} ({a.IATACode})"
                })
                .ToListAsync();

            return new JsonResult(new { results = result });
        }

        private async Task<User> GetUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }


        private async Task LoadSelectedAirportNames()
        {
            if (DepartureAirportId != Guid.Empty)
            {
                var depAirport = await _context.Airports
                    .FirstOrDefaultAsync(a => a.Id == DepartureAirportId);

                if (depAirport != null)
                {
                    SelectedDepartureName =
                        $"{depAirport.Name} - {depAirport.City} ({depAirport.IATACode})";
                }
            }

            if (ArrivalAirportId != Guid.Empty)
            {
                var arrAirport = await _context.Airports
                    .FirstOrDefaultAsync(a => a.Id == ArrivalAirportId);

                if (arrAirport != null)
                {
                    SelectedArrivalName =
                        $"{arrAirport.Name} - {arrAirport.City} ({arrAirport.IATACode})";
                }
            }
        }
    }
}