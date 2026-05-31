using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;

namespace Proiect_Licenta.Areas.Identity.Pages.Account.Manage
{
    public class VouchersModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly VoucherService _voucherService;

        public VouchersModel(UserManager<User> userManager, VoucherService voucherService)
        {
            _userManager = userManager;
            _voucherService = voucherService;
        }

        // Proprietate pentru a stoca lista ce va fi afișată în HTML
        public List<Voucher> UserVouchers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Identificăm userul curent
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // 2. Luăm voucherele lui folosind serviciul tău
            UserVouchers = await _voucherService.GetUserVouchersAsync(user.Id);

            return Page();
        }
    }
}
