using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;
using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Pages
{
    [Authorize]
    public class PostModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly UserProgressService _userProgressService;
        private readonly BadgeService _badgeService;

        public PostModel(ApplicationDbContext context, UserManager<User> userManager,
                         UserProgressService userProgressService, BadgeService badgeService)
        {
            _context = context;
            _userManager = userManager;
            _userProgressService = userProgressService;
            _badgeService = badgeService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(200)]
            public string Title { get; set; }

            [StringLength(2000)]
            public string? Description { get; set; }

            [Required]
            public IFormFile ImageFile { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            // salvare imagine
            string imagePath = "";

            if (Input.ImageFile != null)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ImageFile.CopyToAsync(stream);
                }

                imagePath = "/uploads/" + fileName;
            }

            var post = new Post
            {
                Title = Input.Title,
                Description = Input.Description,
                ImagePath = imagePath,
                UserId = user.Id,
                User = user
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var log = new AdminLog
            {
                Action = "CREATE_POST",
                Details = $"User {user.UserName} created a post titled '{post.Title}'",
                PerformedByUserId = user.Id,
                PerformedByUser = user
            };

            _context.AdminLogs.Add(log);
            await _context.SaveChangesAsync();




            await _userProgressService.AddPointsAsync(user, 50);
            await _badgeService.CheckPostingBadgesAsync(user.Id);





            return RedirectToPage("/Account/Manage/Profile", new { area = "Identity"}); // sau unde vrei
        }
    }
}
