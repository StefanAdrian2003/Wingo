using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class FlightModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public FlightModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public IList<Airport> Airports { get; set; } = new List<Airport>();
        public IList<Flight> Flights { get; set; } = new List<Flight>();

        [BindProperty(SupportsGet = true)]
        public Guid? DepartureAirportId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ArrivalAirportId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool WithLayover { get; set; }
        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        public string SelectedDepartureName { get; set; }
        public string SelectedArrivalName { get; set; }

        public async Task OnGetAsync()
        {
            Airports = await _db.Airports
                .OrderBy(a => a.City)
                .ToListAsync();

            var query = _db.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.Airline)
                    .ThenInclude(a => a.User)
                .AsQueryable();

            if (DepartureAirportId.HasValue)
            {
                query = query.Where(f =>
                    f.DepartureAirportId == DepartureAirportId.Value);
            }

            if (ArrivalAirportId.HasValue)
            {
                query = query.Where(f =>
                    f.ArrivalAirportId == ArrivalAirportId.Value);
            }

            if (MaxPrice.HasValue)
            {
                query = query.Where(f => f.Price <= MaxPrice.Value);
            }

            // momentan doar placeholder
            // mai tarziu faci logica reala pentru escale
            if (!WithLayover)
            {
                // zboruri directe
            }


            if (DepartureAirportId.HasValue)
            {
                var departureAirport = await _db.Airports
                    .FirstOrDefaultAsync(a => a.Id == DepartureAirportId.Value);

                if (departureAirport != null)
                {
                    SelectedDepartureName =
                        $"{departureAirport.Name} - {departureAirport.City} ({departureAirport.IATACode})";
                }
            }

            if (ArrivalAirportId.HasValue)
            {
                var arrivalAirport = await _db.Airports
                    .FirstOrDefaultAsync(a => a.Id == ArrivalAirportId.Value);

                if (arrivalAirport != null)
                {
                    SelectedArrivalName =
                        $"{arrivalAirport.Name} - {arrivalAirport.City} ({arrivalAirport.IATACode})";
                }
            }


            Flights = await query
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();
        }

        public async Task<JsonResult> OnGetAirportsAsync(string term)
        {
            var result = await _db.Airports
                .Where(a =>
                    a.Name.Contains(term) ||
                    a.City.Contains(term) ||
                    a.IATACode.Contains(term))
                .Take(10)
                .Select(a => new
                {
                    id = a.Id,
                    text = $"{a.Name} - {a.City} ({a.IATACode})"
                })
                .ToListAsync();

            return new JsonResult(new { results = result });
        }

    }
}
