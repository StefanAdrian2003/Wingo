using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class FeedModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _db;

        public FeedModel(UserManager<User> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }


        public IList<Post> Posts { get; set; } = new List<Post>();

        public async Task OnGetAsync()
        {
            Posts = await _db.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Likes)
                    .ThenInclude(l => l.User)
                .OrderByDescending(p => p.DateOfCreation)
                .ToListAsync();
        }
    }
}
