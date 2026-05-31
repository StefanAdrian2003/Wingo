using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    [Authorize(Roles ="Company")]
    public class AddAircraftModel : PageModel
    {

        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public AddAircraftModel(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [BindProperty]
        public Aircraft Aircraft { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
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

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Aircraft.Id = Guid.NewGuid();
            Aircraft.AirlineId = user.AirlineId.Value;

            _db.Aircrafts.Add(Aircraft);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Account/Manage/Profile", new { area = "Identity" });
        }
    }
}
