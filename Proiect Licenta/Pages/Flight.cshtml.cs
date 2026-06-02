using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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

        // --- NEW DATE FILTER PROPERTY ---
        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? SelectedDate { get; set; }

        // --- PAGINATION PROPERTIES ---
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public string SelectedDepartureName { get; set; }
        public string SelectedArrivalName { get; set; }

        public async Task OnGetAsync()
        {
            if (CurrentPage < 1) CurrentPage = 1;

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

            // ---------------------------------------------------------------------
            // RECULĂ PRINCIPALĂ: Ascunde zborurile din trecut
            // ---------------------------------------------------------------------
            DateTime now = DateTime.Now;
            query = query.Where(f => f.DepartureTime >= now);

            // Filtre Core
            if (DepartureAirportId.HasValue)
            {
                query = query.Where(f => f.DepartureAirportId == DepartureAirportId.Value);
            }

            if (ArrivalAirportId.HasValue)
            {
                query = query.Where(f => f.ArrivalAirportId == ArrivalAirportId.Value);
            }

            if (MaxPrice.HasValue)
            {
                query = query.Where(f => f.Price <= MaxPrice.Value);
            }

            // Filtru dată calendaristică (ajustat pentru a respecta ora curentă)
            if (SelectedDate.HasValue)
            {
                DateTime startOfDay = SelectedDate.Value.Date;
                DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                // Dacă utilizatorul a selectat ziua de AOARE, intervalul pornește de ACUM, nu de la miezul nopții
                if (startOfDay == now.Date)
                {
                    startOfDay = now;
                }

                query = query.Where(f => f.DepartureTime >= startOfDay && f.DepartureTime <= endOfDay);
            }

            if (!WithLayover)
            {
                // direct flights logic placeholder
            }

            int totalFlightsCount = await query.CountAsync();

            TotalPages = (int)Math.Ceiling(totalFlightsCount / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;

            Flights = await query
                .OrderBy(f => f.DepartureTime)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Încarcă etichetele vizuale pentru dropdown-uri
            if (DepartureAirportId.HasValue)
            {
                var departureAirport = await _db.Airports.FirstOrDefaultAsync(a => a.Id == DepartureAirportId.Value);
                if (departureAirport != null)
                {
                    SelectedDepartureName = $"{departureAirport.Name} - {departureAirport.City} ({departureAirport.IATACode})";
                }
            }

            if (ArrivalAirportId.HasValue)
            {
                var arrivalAirport = await _db.Airports.FirstOrDefaultAsync(a => a.Id == ArrivalAirportId.Value);
                if (arrivalAirport != null)
                {
                    SelectedArrivalName = $"{arrivalAirport.Name} - {arrivalAirport.City} ({arrivalAirport.IATACode})";
                }
            }
        }

        public async Task<JsonResult> OnGetAirportsAsync(string term)
        {
            var result = await _db.Airports
                .Where(a => a.Name.Contains(term) || a.City.Contains(term) || a.IATACode.Contains(term))
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