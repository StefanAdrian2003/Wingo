using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class FlightInfoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FlightInfoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Flight Flight { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft)
                    .ThenInclude(a => a.SeatSections)
                        .ThenInclude(ss => ss.Seats)
                            .ThenInclude(s => s.FlightSeats)  // load all, filter in view
                .FirstOrDefaultAsync(f => f.Id == id);

            if (Flight == null)
                return NotFound();

            return Page();
        }
    }
}