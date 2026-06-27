using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Data.Seeders;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;
using System.Text.RegularExpressions;          
using Microsoft.AspNetCore.RateLimiting;   

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<User>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "private", "keys")));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(policyName: "fixed", fixedOptions =>
    {
        fixedOptions.PermitLimit = 20;
        fixedOptions.Window = TimeSpan.FromSeconds(10);
        fixedOptions.QueueLimit = 0;
    });
});

builder.Services.AddRazorPages();
builder.Services.AddHttpClient<CommentModerationService>();

builder.Services.AddScoped<BadgeService>();
builder.Services.AddScoped<UserProgressService>();
builder.Services.AddScoped<VoucherService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


    var services = scope.ServiceProvider;
    try
    {
        if (!context.Airports.Any())
        {
            var lines = File.ReadAllLines("Data/airports.dat");

            var csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            foreach (var line in lines)
            {
                var parts = csvParser.Split(line);

                if (parts.Length < 8)
                    continue;

                double.TryParse(parts[6].Trim('"'), System.Globalization.CultureInfo.InvariantCulture, out double latitude);
                double.TryParse(parts[7].Trim('"'), System.Globalization.CultureInfo.InvariantCulture, out double longitude);

                var airport = new Airport
                {
                    Name = parts[1].Trim('"'),
                    City = parts[2].Trim('"'),
                    Country = parts[3].Trim('"'),
                    IATACode = parts[4].Trim('"'),
                    ICAOCode = parts[5].Trim('"'),
                    Latitude = latitude,   
                    Longitude = longitude  
                };

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

        string adminEmail = builder.Configuration["SeedSettings:AdminEmail"] ?? "admin@site.com";
        string adminPassword = builder.Configuration["SeedSettings:AdminPassword"] ?? "Admin123!";
        string adminUsername = builder.Configuration["SeedSettings:AdminUsername"] ?? "admin";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user = new User
            {
                LastName = "Stefan",
                FirstName = "Adrian",
                UserName = adminUsername,
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