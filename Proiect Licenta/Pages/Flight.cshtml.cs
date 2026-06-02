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

        // Liste pentru stocarea rezultatelor transmise către HTML
        public IList<Flight> Flights { get; set; } = new List<Flight>();
        public IList<RoundTrip> RoundTrips { get; set; } = new List<RoundTrip>();

        // Filtrele din formular
        [BindProperty(SupportsGet = true)]
        public Guid? DepartureAirportId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ArrivalAirportId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool WithLayover { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsRoundTrip { get; set; } = false; // Proprietatea nouă pentru checkbox

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? SelectedDate { get; set; }

        // Proprietăți pentru paginare
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public string SelectedDepartureName { get; set; }
        public string SelectedArrivalName { get; set; }

        public async Task OnGetAsync()
        {
            if (CurrentPage < 1) CurrentPage = 1;

            // Încărcăm aeroporturile pentru popularea inițială sau fallback
            Airports = await _db.Airports.OrderBy(a => a.City).ToListAsync();

            DateTime now = DateTime.Now;

            // 1. EXTRACTIE ZBORURI DUS (OUTBOUND)
            var outboundQuery = _db.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.Airline).ThenInclude(a => a.User)
                .Where(f => f.DepartureTime >= now)
                .AsQueryable();

            if (DepartureAirportId.HasValue)
                outboundQuery = outboundQuery.Where(f => f.DepartureAirportId == DepartureAirportId.Value);

            if (ArrivalAirportId.HasValue)
                outboundQuery = outboundQuery.Where(f => f.ArrivalAirportId == ArrivalAirportId.Value);

            if (SelectedDate.HasValue)
            {
                DateTime startOfDay = SelectedDate.Value.Date;
                DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);
                if (startOfDay == now.Date) startOfDay = now;

                outboundQuery = outboundQuery.Where(f => f.DepartureTime >= startOfDay && f.DepartureTime <= endOfDay);
            }

            var matchingOutboundFlights = await outboundQuery.ToListAsync();

            // 2. LOGICĂ GENERARE DUS-ÎNTORS SAU DOAR DUS
            if (IsRoundTrip && DepartureAirportId.HasValue && ArrivalAirportId.HasValue)
            {
                // Căutăm zborurile de întoarcere (inversând aeroporturile selectate)
                var returnQuery = _db.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Include(f => f.Aircraft)
                    .Include(f => f.Airline).ThenInclude(a => a.User)
                    .Where(f => f.DepartureAirportId == ArrivalAirportId.Value &&
                                f.ArrivalAirportId == DepartureAirportId.Value)
                    .AsQueryable();

                var matchingReturnFlights = await returnQuery.ToListAsync();

                var dynamicPairs = new List<RoundTrip>();
                foreach (var outbound in matchingOutboundFlights)
                {
                    // Zborul de întoarcere trebuie să plece după ce zborul de dus a aterizat
                    var validReturns = matchingReturnFlights
                        .Where(ret => ret.DepartureTime > outbound.ArrivalTime);

                    foreach (var inbound in validReturns)
                    {
                        var roundTrip = new RoundTrip
                        {
                            OutboundFlight = outbound,
                            ReturnFlight = inbound
                        };

                        // Verificăm filtrul de preț maxim aplicat pe prețul total redus al pachetului
                        if (!MaxPrice.HasValue || roundTrip.TotalPrice <= MaxPrice.Value)
                        {
                            dynamicPairs.Add(roundTrip);
                        }
                    }
                }

                int totalPairsCount = dynamicPairs.Count;
                TotalPages = (int)Math.Ceiling(totalPairsCount / (double)PageSize);
                if (TotalPages == 0) TotalPages = 1;

                RoundTrips = dynamicPairs
                    .OrderBy(rt => rt.OutboundFlight.DepartureTime)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Golim lista de zboruri simple pentru siguranța interfeței
                Flights = new List<Flight>();
            }
            else
            {
                // Modul standard: Doar Dus
                if (MaxPrice.HasValue)
                    outboundQuery = outboundQuery.Where(f => f.Price <= MaxPrice.Value);

                int totalSingleCount = outboundQuery.Count();
                TotalPages = (int)Math.Ceiling(totalSingleCount / (double)PageSize);
                if (TotalPages == 0) TotalPages = 1;

                Flights = await outboundQuery
                    .OrderBy(f => f.DepartureTime)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Golim lista de round trips
                RoundTrips = new List<RoundTrip>();
            }

            await HydrateSelectionLabels();
        }

        private async Task HydrateSelectionLabels()
        {
            if (DepartureAirportId.HasValue)
            {
                var dep = await _db.Airports.FirstOrDefaultAsync(a => a.Id == DepartureAirportId.Value);
                if (dep != null) SelectedDepartureName = $"{dep.Name} - {dep.City} ({dep.IATACode})";
            }
            if (ArrivalAirportId.HasValue)
            {
                var arr = await _db.Airports.FirstOrDefaultAsync(a => a.Id == ArrivalAirportId.Value);
                if (arr != null) SelectedArrivalName = $"{arr.Name} - {arr.City} ({arr.IATACode})";
            }
        }

        public async Task<JsonResult> OnGetAirportsAsync(string term)
        {
            var result = await _db.Airports
                .Where(a => a.Name.Contains(term) || a.City.Contains(term) || a.IATACode.Contains(term))
                .Take(10)
                .Select(a => new { id = a.Id, text = $"{a.Name} - {a.City} ({a.IATACode})" })
                .ToListAsync();

            return new JsonResult(new { results = result });
        }
    }
}