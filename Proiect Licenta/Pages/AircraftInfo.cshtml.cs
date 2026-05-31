using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class AircraftInfoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AircraftInfoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Aircraft Aircraft { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Aircraft = await _context.Aircrafts
                .Include(a => a.Airline)
                    .ThenInclude(a => a.User)
                .Include(a => a.SeatSections)
                    .ThenInclude(ss => ss.Seats)
                .Include(a => a.Flights)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (Aircraft == null)
                return NotFound();

            return Page();
        }
    }
}
