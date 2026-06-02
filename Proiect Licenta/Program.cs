using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;
using Proiect_Licenta.Data.Seeders;

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

    // https://openflights.org/data  DE AICI AM LUAT ZBORURILE
    var context = scope.ServiceProvider
    .GetRequiredService<ApplicationDbContext>();

    await context.Database.MigrateAsync();



    var services = scope.ServiceProvider;
    try
    {

        if (!context.Airports.Any())
        {
            var lines = File.ReadAllLines("Data/airports.dat");

            foreach (var line in lines)
            {
                var parts = line.Split(',');

                if (parts.Length < 5)
                    continue;

                var airport = new Airport
                {
                    Name = parts[1].Trim('"'),
                    City = parts[2].Trim('"'),
                    Country = parts[3].Trim('"'),
                    IATACode = parts[4].Trim('"'),
                    ICAOCode = parts[5].Trim('"')
                };

                // ignoră aeroporturile fără IATA
                // ignoră aeroporturile incomplete
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

        // Creează rolurile dacă nu există
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Creează admin dacă nu există
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

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding roles and admin: {ex.Message}");
    }
}




app.Run();
