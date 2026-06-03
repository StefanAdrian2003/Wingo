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

        // Lists for storing the results sent to HTML
        public IList<Flight> Flights { get; set; } = new List<Flight>();
        public IList<RoundTrip> RoundTrips { get; set; } = new List<RoundTrip>();
        public IList<ConnectingFlight> ConnectingFlights { get; set; } = new List<ConnectingFlight>();

        // Form Filters
        [BindProperty(SupportsGet = true)]
        public Guid? DepartureAirportId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ArrivalAirportId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool WithLayover { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsRoundTrip { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? SelectedDate { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        // Pagination Properties
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

            Airports = await _db.Airports.OrderBy(a => a.City).ToListAsync();
            DateTime now = DateTime.Now;

            // 1. BASE OUTBOUND QUERY
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

            // 2. ROUND-TRIP LOGIC
            if (IsRoundTrip && DepartureAirportId.HasValue && ArrivalAirportId.HasValue)
            {
                var returnQuery = _db.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Include(f => f.Aircraft)
                    .Include(f => f.Airline).ThenInclude(a => a.User)
                    .Where(f => f.DepartureAirportId == ArrivalAirportId.Value &&
                                 f.ArrivalAirportId == DepartureAirportId.Value)
                    .AsQueryable();

                if (ReturnDate.HasValue)
                {
                    DateTime startOfReturn = ReturnDate.Value.Date;
                    DateTime endOfReturn = startOfReturn.AddDays(1).AddTicks(-1);
                    returnQuery = returnQuery.Where(f => f.DepartureTime >= startOfReturn && f.DepartureTime <= endOfReturn);
                }
                else
                {
                    if (matchingOutboundFlights.Any())
                    {
                        var minOutboundArrival = matchingOutboundFlights.Min(f => f.ArrivalTime);
                        returnQuery = returnQuery.Where(f => f.DepartureTime > minOutboundArrival);
                    }
                }

                var matchingReturnFlights = await returnQuery.ToListAsync();
                var dynamicPairs = new List<RoundTrip>();

                foreach (var outbound in matchingOutboundFlights)
                {
                    var validReturns = matchingReturnFlights.Where(ret => ret.DepartureTime > outbound.ArrivalTime);
                    foreach (var inbound in validReturns)
                    {
                        var roundTrip = new RoundTrip { OutboundFlight = outbound, ReturnFlight = inbound };
                        if (!MaxPrice.HasValue || roundTrip.TotalPrice <= MaxPrice.Value)
                        {
                            dynamicPairs.Add(roundTrip);
                        }
                    }
                }

                TotalPages = (int)Math.Ceiling(dynamicPairs.Count / (double)PageSize);
                if (TotalPages == 0) TotalPages = 1;

                RoundTrips = dynamicPairs
                    .OrderBy(rt => rt.OutboundFlight.DepartureTime)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            // 3. ONE-WAY / LAYOVER FALLBACK LOGIC
            else
            {
                if (MaxPrice.HasValue)
                    outboundQuery = outboundQuery.Where(f => f.Price <= MaxPrice.Value);

                var directFlights = await outboundQuery.ToListAsync();

                // If we found direct flights, show them!
                if (directFlights.Any())
                {
                    TotalPages = (int)Math.Ceiling(directFlights.Count / (double)PageSize);
                    if (TotalPages == 0) TotalPages = 1;

                    Flights = directFlights
                        .OrderBy(f => f.DepartureTime)
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                }
                // FALLBACK: No direct flights. Let's look for layovers.
                else if (WithLayover && DepartureAirportId.HasValue && ArrivalAirportId.HasValue && SelectedDate.HasValue)
                {
                    DateTime startOfDay = SelectedDate.Value.Date;
                    DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                    var firstLegs = await _db.Flights
                        .Include(f => f.DepartureAirport)
                        .Include(f => f.ArrivalAirport)
                        .Include(f => f.Aircraft)
                        .Include(f => f.Airline).ThenInclude(a => a.User)
                        .Where(f => f.DepartureAirportId == DepartureAirportId.Value 
                                 && f.DepartureTime >= startOfDay 
                                 && f.DepartureTime <= endOfDay)
                        .ToListAsync();

                    var secondLegs = await _db.Flights
                        .Include(f => f.DepartureAirport)
                        .Include(f => f.ArrivalAirport)
                        .Include(f => f.Aircraft)
                        .Include(f => f.Airline).ThenInclude(a => a.User)
                        .Where(f => f.ArrivalAirportId == ArrivalAirportId.Value 
                                 && f.DepartureTime >= startOfDay)
                        .ToListAsync();

                    var foundLayovers = new List<ConnectingFlight>();

                    foreach (var leg1 in firstLegs)
                    {
                        foreach (var leg2 in secondLegs)
                        {
                            if (leg1.ArrivalAirportId == leg2.DepartureAirportId)
                            {
                                TimeSpan layoverTime = leg2.DepartureTime - leg1.ArrivalTime;

                                // Layover must be between 1 and 12 hours
                                if (layoverTime.TotalHours >= 1 && layoverTime.TotalHours <= 12)
                                {
                                    var connectingFlight = new ConnectingFlight { Leg1 = leg1, Leg2 = leg2 };
                                    if (!MaxPrice.HasValue || connectingFlight.TotalPrice <= MaxPrice.Value)
                                    {
                                        foundLayovers.Add(connectingFlight);
                                    }
                                }
                            }
                        }
                    }

                    TotalPages = (int)Math.Ceiling(foundLayovers.Count / (double)PageSize);
                    if (TotalPages == 0) TotalPages = 1;

                    ConnectingFlights = foundLayovers
                        .OrderBy(l => l.Leg1.DepartureTime)
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                }
                else
                {
                    TotalPages = 1;
                    Flights = new List<Flight>();
                }
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