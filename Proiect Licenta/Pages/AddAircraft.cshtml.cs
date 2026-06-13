using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Pages
{
    [Authorize(Roles = "Company")]
    public class AddAircraftModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public AddAircraftModel(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Bind ONLY flat, primitive inputs from the form
        [BindProperty]
        [Required(ErrorMessage = "The Aircraft Model name is required.")]
        [Display(Name = "Aircraft Model Name")]
        public string AircraftModelName { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "First Class Rows")]
        public int FirstClassRows { get; set; } = 2;

        [BindProperty]
        public string FirstClassLayout { get; set; } = "AB-CD";

        [BindProperty]
        [Display(Name = "Business Class Rows")]
        public int BusinessClassRows { get; set; } = 4;

        [BindProperty]
        public string BusinessClassLayout { get; set; } = "ABC-DEF";

        [BindProperty]
        [Display(Name = "Economy Class Rows")]
        public int EconomyClassRows { get; set; } = 20;

        [BindProperty]
        public string EconomyClassLayout { get; set; } = "ABC-DEF";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Safety check: Ensure the user is actually authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Challenge(); // Redirects to the Login page
            }


            var userId = _userManager.GetUserId(User);

            var user = _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.AirlineId })
                .FirstOrDefault();

            if (user == null || user.AirlineId == null)
            {
                ModelState.AddModelError("", "User is not associated with an airline.");
                return Page();
            }

            if (FirstClassRows == 0 && BusinessClassRows == 0 && EconomyClassRows == 0)
            {
                ModelState.AddModelError("", "An aircraft must contain at least 1 row of any travel class.");
                return Page();
            }

            // This now checks ONLY your primitives. No navigation properties can break it!
            if (!ModelState.IsValid)
            {
                return Page();
            }

            DateTime creationTime = DateTime.UtcNow;

            // 1. Create the database object internally right here
            var aircraft = new Aircraft
            {
                Id = Guid.NewGuid(),
                Model = AircraftModelName, // Set from our flat string property
                AirlineId = user.AirlineId.Value,
                TotalSeats = 0,
                DateOfCreation = creationTime
            };

            int currentRowIndex = 1;

            // 2. Generation processing loop
            void BuildSection(TravelClass travelClass, int totalRows, string layout)
            {
                if (totalRows <= 0 || string.IsNullOrWhiteSpace(layout)) return;

                int seatsPerRow = layout.Replace("-", "").Length;
                if (seatsPerRow == 0) return;

                int endRow = currentRowIndex + totalRows - 1;

                var section = new SeatSection
                {
                    Id = Guid.NewGuid(),
                    AircraftId = aircraft.Id,
                    TravelClass = travelClass,
                    StartRow = currentRowIndex,
                    EndRow = endRow,
                    Layout = layout,
                    DateOfCreation = creationTime
                };

                _db.SeatSections.Add(section);

                for (int row = currentRowIndex; row <= endRow; row++)
                {
                    foreach (var block in layout.Split('-'))
                    {
                        foreach (char letter in block)
                        {
                            var seat = new Seat
                            {
                                Id = Guid.NewGuid(),
                                SeatSectionId = section.Id,
                                TravelClass = travelClass,
                                SeatNumber = $"{row}{letter}",
                                DateOfCreation = creationTime
                            };
                            _db.Seats.Add(seat);

                            aircraft.TotalSeats++;
                        }
                    }
                }

                currentRowIndex = endRow + 1;
            }

            // Build dynamic items using our processing properties
            BuildSection(TravelClass.First, FirstClassRows, FirstClassLayout);
            BuildSection(TravelClass.Business, BusinessClassRows, BusinessClassLayout);
            BuildSection(TravelClass.Economy, EconomyClassRows, EconomyClassLayout);

            // 3. Persist context shifts
            _db.Aircrafts.Add(aircraft);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Account/Manage/Profile", new { area = "Identity" });
        }
    }
}