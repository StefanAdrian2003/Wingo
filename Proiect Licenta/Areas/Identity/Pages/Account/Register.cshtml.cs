// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Proiect_Licenta.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            // 🛡️ SECURITY LAYER: Explicit input parameter for custom unique pseudonyms
            [Required]
            [StringLength(30, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$", ErrorMessage = "Username can only contain letters, numbers, dots, hyphens, and underscores.")]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string FirstName { get; set; }

            [Required]
            public string LastName { get; set; }

            // Proprietăți pentru cont companie
            public string CompanyName { get; set; }
            public string IATACode { get; set; }
            public string Country { get; set; }
            public IFormFile LogoFile { get; set; }

            // Tipul contului
            public bool IsCompany { get; set; } = false;
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (Input.IsCompany)
            {
                if (string.IsNullOrWhiteSpace(Input.CompanyName))
                    ModelState.AddModelError("Input.CompanyName", "Company Name is required");

                if (string.IsNullOrWhiteSpace(Input.IATACode))
                    ModelState.AddModelError("Input.IATACode", "IATA Code is required");

                if (string.IsNullOrWhiteSpace(Input.Country))
                    ModelState.AddModelError("Input.Country", "Country is required");

                if (Input.LogoFile == null)
                    ModelState.AddModelError("Input.LogoFile", "Logo is required");

                bool airlineExists = _context.Airlines
                .Any(a =>
                    a.Name.ToLower() == (Input.CompanyName ?? "").ToLower() &&
                    a.IATACode.ToLower() == (Input.IATACode ?? "").ToLower() &&
                    a.Country.ToLower() == (Input.Country ?? "").ToLower()
                    );

                if (airlineExists)
                {
                    ModelState.AddModelError(string.Empty, "A company with these details already exists.");
                }
            }

            if (ModelState.IsValid)
            {
                // 🛡️ DEFENSIVE PROGRAMMING CHECK: Catch duplicate usernames before file manipulation or entity writing
                var usernameExists = await _userManager.FindByNameAsync(Input.Username);
                if (usernameExists != null)
                {
                    ModelState.AddModelError("Input.Username", "This username is already taken. Please choose another one.");
                    return Page();
                }

                var user = CreateUser();

                // 🛡️ Map the custom chosen string moniker as the public app username
                await _userStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Dacă este cont de companie, creează și Airline
                    if (Input.IsCompany)
                    {
                        await _userManager.AddToRoleAsync(user, "Company");

                        string LogoUrl = "";

                        if (Input.LogoFile != null)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.LogoFile.FileName);

                            // Use _environment.WebRootPath to get the absolute path to wwwroot
                            var logosFolder = Path.Combine(_environment.WebRootPath, "Logos");

                            // CRITICAL GUARD: Ensure the folder exists on the server
                            if (!Directory.Exists(logosFolder))
                            {
                                Directory.CreateDirectory(logosFolder);
                            }

                            var filePath = Path.Combine(logosFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await Input.LogoFile.CopyToAsync(stream);
                            }

                            LogoUrl = "/Logos/" + fileName;
                        }

                        var airline = new Airline
                        {
                            Name = Input.CompanyName,
                            IATACode = Input.IATACode,
                            Country = Input.Country,
                            LogoUrl = LogoUrl,
                            UserId = user.Id,
                            User = user
                        };

                        _context.Airlines.Add(airline);

                        user.AirlineId = airline.Id;
                        user.Airline = airline;

                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private User CreateUser()
        {
            try
            {
                var user = Activator.CreateInstance<User>();
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.IsCompany = Input.IsCompany;
                return user;
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}