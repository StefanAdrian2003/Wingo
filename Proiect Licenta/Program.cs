using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Proiect_Licenta.Data;
using Proiect_Licenta.Data.Seeders;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;
using System.IO;                       // 🛡️ Required for Data Protection file paths
using System.Text.RegularExpressions; // Required for CSV Quote-Safe Parser Engine

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 🛡️ FIX FOR SHARED HOSTING: Forces Antiforgery/DataProtection keys to save to a permanent file directory
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "private", "keys")));

builder.Services.AddRazorPages();
builder.Services.AddHttpClient<CommentModerationService>();

builder.Services.AddScoped<BadgeService>();
builder.Services.AddScoped<UserProgressService>();
builder.Services.AddScoped<VoucherService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await context.Database.MigrateAsync();

    var services = scope.ServiceProvider;
    try
    {
        if (!context.Airports.Any())
        {
            var lines = File.ReadAllLines("Data/airports.dat");

            // Regular Expression to match commas ONLY outside of double quotes
            var csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            foreach (var line in lines)
            {
                // Quote-safe array extraction
                var parts = csvParser.Split(line);

                if (parts.Length < 8) // Valid rows must reach at least index position 7 for longitude
                    continue;

                // Safely extract and parse numeric coordinates from indexes 6 and 7
                double.TryParse(parts[6].Trim('"'), System.Globalization.CultureInfo.InvariantCulture, out double latitude);
                double.TryParse(parts[7].Trim('"'), System.Globalization.CultureInfo.InvariantCulture, out double longitude);

                var airport = new Airport
                {
                    Name = parts[1].Trim('"'),
                    City = parts[2].Trim('"'),
                    Country = parts[3].Trim('"'),
                    IATACode = parts[4].Trim('"'),
                    ICAOCode = parts[5].Trim('"'),
                    Latitude = latitude,   // Assigned
                    Longitude = longitude  // Assigned
                };

                // Filter logic blocks
                if (string.IsNullOrWhiteSpace(airport.Name) ||
                    string.IsNullOrWhiteSpace(airport.City) ||
                    string.IsNullOrWhiteSpace(airport.IATACode) ||
                    airport.IATACode == "\\N")
                {
                    continue;
                }

                context.Airports.Add(airport);
            }

            context.SaveChanges();
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        string[] roles = { "Admin", "Company", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        string adminEmail = "admin@site.com";
        string adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user = new User
            {
                LastName = "Stefan",
                FirstName = "Adrian",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        await CompanySeeder.SeedCompaniesAsync(context, userManager);
        await TestLayoverSeeder.SeedAsync(context, userManager);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding roles and admin: {ex.Message}");
    }
}

app.Run();