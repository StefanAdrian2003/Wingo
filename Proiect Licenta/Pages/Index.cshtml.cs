using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proiect_Licenta.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public int FlightsCount { get; set; }
        public int MembersCount { get; set; }
        public int BookingsCount { get; set; }
        public int BadgesEarnedCount { get; set; }

        public List<Flight> PopularFlights { get; set; } = new();
        public List<Post> LatestPosts { get; set; } = new();
        public List<Badge> FeaturedBadges { get; set; } = new();

        public async Task OnGetAsync()
        {
            FlightsCount = await _context.Flights.CountAsync();

            MembersCount = await _context.Users
                .Where(u => !u.IsCompany)
                .CountAsync();

            BookingsCount = await _context.Bookings.CountAsync();

            BadgesEarnedCount = await _context.UserBadges.CountAsync();

            // Filtrare: doar zboruri care se decolează după momentul actual curent
            PopularFlights = await _context.Flights
                .Include(f => f.Airline)
                    .ThenInclude(a => a.User)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.FlightSeats)
                .Where(f => f.Aircraft.TotalSeats > 0 && f.DepartureTime > DateTime.Now)
                .OrderByDescending(f =>
                    f.FlightSeats.Count(fs => fs.TicketId != null) == 0 ? 0 :
                    ((double)f.FlightSeats.Count(fs => fs.TicketId != null) / f.Aircraft.TotalSeats) * 100)
                .Take(6)
                .ToListAsync();

            LatestPosts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .OrderByDescending(p => p.DateOfCreation)
                .Take(2)
                .ToListAsync();

            FeaturedBadges = await _context.Badges
                .Take(6)
                .ToListAsync();
        }
    }
}