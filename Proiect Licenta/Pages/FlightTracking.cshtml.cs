using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models; // Make sure your models namespace matches
using System;
using System.Threading.Tasks;

namespace Proiect_Licenta.Pages
{
    public class FlightTrackingModel : PageModel
    {
        private readonly ApplicationDbContext _context; // Your EF DbContext

        public FlightTrackingModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string FlightNumber { get; set; }

        // Live Data payload sent back to Leaflet.js
        public bool FlightFound { get; set; }
        public string FlightCode { get; set; }
        public string DepartureAirportName { get; set; }
        public string ArrivalAirportName { get; set; }
        public double DepLat { get; set; }
        public double DepLng { get; set; }
        public double ArrLat { get; set; }
        public double ArrLng { get; set; }
        public double ProgressPercent { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(FlightNumber))
            {
                FlightFound = false;
                return Page();
            }

            string cleanFlightNum = FlightNumber.Trim().ToUpper();

            // Query database and Eagerly Load (.Include) the Airport navigation properties
            var flight = await _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.FlightNumber.ToUpper() == cleanFlightNum);

            if (flight == null)
            {
                FlightFound = false;
                return Page();
            }

            // Map data from your database model directly
            FlightFound = true;
            FlightCode = flight.FlightNumber;
            DepartureAirportName = flight.DepartureAirport.Name; // Assumes your Airport model has a 'Name' or 'Code' property
            ArrivalAirportName = flight.ArrivalAirport.Name;

            // Extract the coordinates stored in your Airport entity
            // (Assumes your Airport model contains Latitude and Longitude fields as double/decimal)
            DepLat = Convert.ToDouble(flight.DepartureAirport.Latitude);
            DepLng = Convert.ToDouble(flight.DepartureAirport.Longitude);
            ArrLat = Convert.ToDouble(flight.ArrivalAirport.Latitude);
            ArrLng = Convert.ToDouble(flight.ArrivalAirport.Longitude);

            // Calculate exact relative progress based on real DB times
            CalculateProgress(flight.DepartureTime, flight.ArrivalTime);

            return Page();
        }

        private void CalculateProgress(DateTime dep, DateTime arr)
        {
            DateTime now = DateTime.UtcNow; // Or DateTime.Now depending on how you seed your times

            if (now <= dep) { ProgressPercent = 0.0; }
            else if (now >= arr) { ProgressPercent = 1.0; }
            else
            {
                double totalDuration = (arr - dep).TotalSeconds;
                double timePassed = (now - dep).TotalSeconds;
                ProgressPercent = timePassed / totalDuration;
            }
        }
    }
}