using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System.ComponentModel.DataAnnotations;

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
        public IList<RoundTrip> RoundTrips { get; set; } = new List<RoundTrip>();
        public IList<ConnectingFlight> ConnectingFlights { get; set; } = new List<ConnectingFlight>();
        public IList<RoundTripConnecting> RoundTripConnectings { get; set; } = new List<RoundTripConnecting>();

        [BindProperty(SupportsGet = true)] public Guid? DepartureAirportId { get; set; }
        [BindProperty(SupportsGet = true)] public Guid? ArrivalAirportId { get; set; }
        [BindProperty(SupportsGet = true)] public bool IsRoundTrip { get; set; } = false;
        [BindProperty(SupportsGet = true)] public decimal? MaxPrice { get; set; }
        [BindProperty(SupportsGet = true)][DataType(DataType.Date)] public DateTime? SelectedDate { get; set; }
        [BindProperty(SupportsGet = true)][DataType(DataType.Date)] public DateTime? ReturnDate { get; set; }

        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public string SelectedDepartureName { get; set; } = "";
        public string SelectedArrivalName { get; set; } = "";

        public async Task OnGetAsync()
        {
            if (CurrentPage < 1) CurrentPage = 1;

            DateTime nowUtc = DateTime.UtcNow;

            var outboundQuery = _db.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.Airline).ThenInclude(a => a.User)
                .Where(f => f.DepartureTime >= nowUtc)
                .AsQueryable();

            if (DepartureAirportId.HasValue)
                outboundQuery = outboundQuery.Where(f => f.DepartureAirportId == DepartureAirportId.Value);
            if (ArrivalAirportId.HasValue)
                outboundQuery = outboundQuery.Where(f => f.ArrivalAirportId == ArrivalAirportId.Value);
            if (SelectedDate.HasValue)
            {
                var start = SelectedDate.Value.Date;
                var end = start.AddDays(1).AddTicks(-1);
                if (start <= nowUtc.Date) start = nowUtc;
                outboundQuery = outboundQuery.Where(f => f.DepartureTime >= start && f.DepartureTime <= end);
            }
            if (MaxPrice.HasValue)
                outboundQuery = outboundQuery.Where(f => f.Price <= MaxPrice.Value);

            var directOutbound = await outboundQuery.ToListAsync();

            if (IsRoundTrip && DepartureAirportId.HasValue && ArrivalAirportId.HasValue)
            {
                if (directOutbound.Any())
                {
                    // Direct outbound exists → look for a direct return.
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
                        var rs = ReturnDate.Value.Date;
                        var re = rs.AddDays(1).AddTicks(-1);
                        returnQuery = returnQuery.Where(f => f.DepartureTime >= rs && f.DepartureTime <= re);
                    }
                    else
                    {
                        var minArrival = directOutbound.Min(f => f.ArrivalTime);
                        returnQuery = returnQuery.Where(f => f.DepartureTime > minArrival);
                    }

                    var directReturn = await returnQuery.ToListAsync();
                    var pairs = new List<RoundTrip>();

                    foreach (var outbound in directOutbound)
                        foreach (var ret in directReturn.Where(r => r.DepartureTime > outbound.ArrivalTime))
                        {
                            var rt = new RoundTrip { OutboundFlight = outbound, ReturnFlight = ret };
                            if (!MaxPrice.HasValue || rt.TotalPrice <= MaxPrice.Value)
                                pairs.Add(rt);
                        }

                    TotalPages = Math.Max(1, (int)Math.Ceiling(pairs.Count / (double)PageSize));
                    RoundTrips = pairs
                        .OrderBy(r => r.OutboundFlight.DepartureTime)
                        .Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                }
                else
                {
                    // No direct outbound → layover round trip.
                    var outboundLayovers = await BuildLayovers(
                        DepartureAirportId.Value, ArrivalAirportId.Value, SelectedDate, nowUtc);

                    // KEY FIX: pass ReturnDate directly (may be null).
                    // When null, BuildLayovers skips the date window and just requires
                    // departure after nowUtc — so D+5 return flights are found correctly.
                    // The old code passed SelectedDate.AddDays(1) which created a D+2
                    // window, completely missing flights seeded on D+5.
                    var returnLayovers = await BuildLayovers(
                        ArrivalAirportId.Value, DepartureAirportId.Value, ReturnDate, nowUtc);

                    var combos = new List<RoundTripConnecting>();
                    foreach (var ol in outboundLayovers)
                        foreach (var rl in returnLayovers.Where(r => r.Leg1.DepartureTime > ol.Leg2.ArrivalTime))
                        {
                            var combo = new RoundTripConnecting { OutboundJourney = ol, ReturnJourney = rl };
                            if (!MaxPrice.HasValue || combo.TotalPrice <= MaxPrice.Value)
                                combos.Add(combo);
                        }

                    TotalPages = Math.Max(1, (int)Math.Ceiling(combos.Count / (double)PageSize));
                    RoundTripConnectings = combos
                        .OrderBy(c => c.OutboundJourney.Leg1.DepartureTime)
                        .Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                }
            }
            else
            {
                // One-way: direct first, then auto-fallback to layovers.
                if (directOutbound.Any())
                {
                    TotalPages = Math.Max(1, (int)Math.Ceiling(directOutbound.Count / (double)PageSize));
                    Flights = directOutbound
                        .OrderBy(f => f.DepartureTime)
                        .Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                }
                else if (DepartureAirportId.HasValue && ArrivalAirportId.HasValue)
                {
                    var layovers = await BuildLayovers(
                        DepartureAirportId.Value, ArrivalAirportId.Value, SelectedDate, nowUtc);

                    if (MaxPrice.HasValue)
                        layovers = layovers.Where(l => l.TotalPrice <= MaxPrice.Value).ToList();

                    TotalPages = Math.Max(1, (int)Math.Ceiling(layovers.Count / (double)PageSize));
                    ConnectingFlights = layovers
                        .OrderBy(l => l.Leg1.DepartureTime)
                        .Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                }
                else
                {
                    TotalPages = 1;
                }
            }

            await HydrateSelectionLabels();
        }

        private async Task<List<ConnectingFlight>> BuildLayovers(
            Guid fromId, Guid toId, DateTime? date, DateTime nowUtc)
        {
            IQueryable<Flight> leg1Query = _db.Flights
                .Include(f => f.DepartureAirport).Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft).Include(f => f.Airline).ThenInclude(a => a.User)
                .Where(f => f.DepartureAirportId == fromId && f.DepartureTime >= nowUtc);

            if (date.HasValue)
            {
                // Only apply a day window when the user explicitly picked a date.
                var start = date.Value.Date;
                var end = start.AddDays(1).AddTicks(-1);
                if (start <= nowUtc.Date) start = nowUtc;
                leg1Query = leg1Query.Where(f => f.DepartureTime >= start && f.DepartureTime <= end);
            }

            var firstLegs = await leg1Query.ToListAsync();
            if (!firstLegs.Any()) return new List<ConnectingFlight>();

            var earliestLeg1Arrival = firstLegs.Min(f => f.ArrivalTime);

            var secondLegs = await _db.Flights
                .Include(f => f.DepartureAirport).Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft).Include(f => f.Airline).ThenInclude(a => a.User)
                .Where(f => f.ArrivalAirportId == toId &&
                            f.DepartureTime >= earliestLeg1Arrival)
                .ToListAsync();

            var result = new List<ConnectingFlight>();
            foreach (var leg1 in firstLegs)
                foreach (var leg2 in secondLegs.Where(l => l.DepartureAirportId == leg1.ArrivalAirportId))
                {
                    var layover = leg2.DepartureTime - leg1.ArrivalTime;
                    if (layover.TotalMinutes >= 45 && layover.TotalHours <= 12)
                        result.Add(new ConnectingFlight { Leg1 = leg1, Leg2 = leg2 });
                }

            return result;
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