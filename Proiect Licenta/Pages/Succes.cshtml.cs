using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proiect_Licenta.Pages
{
    public class SuccesModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = "/";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            return Redirect(ReturnUrl);
        }
    }
}
