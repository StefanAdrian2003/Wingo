using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Data.Seeders
{
    public static class CompanySeeder
    {
        public static async Task SeedCompaniesAsync(
                ApplicationDbContext context,
                UserManager<User> userManager)
        {
            var random = new Random();

            var airports = await context.Airports
                .Where(a =>
                    a.Country == "Romania" ||
                    a.Country == "France" ||
                    a.Country == "Germany" ||
                    a.Country == "Italy" ||
                    a.Country == "Spain" ||
                    a.Country == "United Kingdom" ||
                    a.Country == "Turkey" ||
                    a.Country == "Netherlands" ||
                    a.Country == "Hungary" ||
                    a.Country == "Austria")
                .ToListAsync();

            var priorityAirports = airports
                .Where(a =>
                    a.IATACode == "OTP" || a.IATACode == "CLJ" ||
                    a.IATACode == "IAS" || a.IATACode == "TSR" ||
                    a.IATACode == "LTN" || a.IATACode == "CDG" ||
                    a.IATACode == "FCO" || a.IATACode == "AMS" ||
                    a.IATACode == "IST" || a.IATACode == "MAD")
                .ToList();

            if (!airports.Any() || !priorityAirports.Any())
                throw new InvalidOperationException("Run AirportSeeder before CompanySeeder.");

            var companies = new List<(string Name, string IATA, string Country)>
            {
                ("Lufthansa",          "LH", "Germany"),
                ("Air France",         "AF", "France"),
                ("KLM",                "KL", "Netherlands"),
                ("Emirates",           "EK", "UAE"),
                ("Qatar Airways",      "QR", "Qatar"),
                ("Turkish Airlines",   "TK", "Turkey"),
                ("Ryanair",            "FR", "Ireland"),
                ("Wizz Air",           "W6", "Hungary"),
                ("British Airways",    "BA", "United Kingdom"),
                ("ITA Airways",        "AZ", "Italy"),
                ("Tarom",              "RO", "Romania"),
                ("Aegean Airlines",    "A3", "Greece"),
                ("Austrian Airlines",  "OS", "Austria"),
                ("Swiss",              "LX", "Switzerland"),
                ("Iberia",             "IB", "Spain"),
                ("easyJet",            "U2", "United Kingdom"),
                ("Finnair",            "AY", "Finland"),
                ("LOT Polish Airlines","LO", "Poland"),
                ("SAS",                "SK", "Sweden"),
                ("Brussels Airlines",  "SN", "Belgium")
            };

            string[] aircraftModels =
            {
                "Airbus A320", "Boeing 737", "Airbus A321", "Boeing 777",
                "Embraer E190", "Airbus A330", "ATR 72", "Boeing 787 Dreamliner"
            };

            // seats-per-row matching layouts — seat counts will be exact multiples
            const int firstPerRow = 4; // "AB-CD"
            const int businessPerRow = 6; // "ABC-DEF"
            const int economyPerRow = 6; // "ABC-DEF"

            foreach (var company in companies)
            {
                var email = $"{company.Name.Replace(" ", "").ToLower()}@wingo.com";
                // Generăm username-ul curat din numele companiei (ex: "lufthansa", "airfrance", "wizzair")
                var username = company.Name.Replace(" ", "").ToLower();

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null)
                    continue;

                var user = new User
                {
                    FirstName = company.Name,
                    LastName = "Official",
                    UserName = username, // Modificat aici: Acum username-ul este numele companiei formatat curat
                    Email = email,
                    EmailConfirmed = true,
                    IsCompany = true
                };

                var result = await userManager.CreateAsync(user, "Company123!");
                if (!result.Succeeded)
                    continue;

                await userManager.AddToRoleAsync(user, "Company");

                var airline = new Airline
                {
                    Name = company.Name,
                    IATACode = company.IATA,
                    Country = company.Country,
                    LogoUrl = $"/Logos/{company.Name}.png",
                    UserId = user.Id,
                    User = user
                };

                context.Airlines.Add(airline);
                user.Airline = airline;
                user.AirlineId = airline.Id;

                var aircraftList = new List<Aircraft>();

                for (int i = 0; i < 5; i++)
                {
                    int rawSeats = random.Next(10, 27); // * 12 = always divisible by both 4 and 6
                    var aircraft = new Aircraft
                    {
                        Model = aircraftModels[random.Next(aircraftModels.Length)],
                        TotalSeats = rawSeats * 12,
                        Airline = airline,
                        AirlineId = airline.Id
                    };

                    aircraftList.Add(aircraft);
                    context.Aircrafts.Add(aircraft);
                }

                await context.SaveChangesAsync();

                foreach (var aircraft in aircraftList)
                {
                    int total = aircraft.TotalSeats;

                    // calculate ROWS first → seat count = rows * seatsPerRow (no remainder)
                    int firstRows = Math.Max(1, (int)(total * 0.10) / firstPerRow);
                    int businessRows = Math.Max(1, (int)(total * 0.20) / businessPerRow);
                    int economyRows = Math.Max(1, (total / economyPerRow) - firstRows - businessRows);

                    int firstSeats = firstRows * firstPerRow;
                    int businessSeats = businessRows * businessPerRow;
                    int economySeats = economyRows * economyPerRow;

                    int currentRow = 1;

                    void CreateSection(TravelClass travelClass, int seatCount, string layout)
                    {
                        if (seatCount <= 0) return;

                        int seatsPerRow = layout.Replace("-", "").Length;
                        int rows = seatCount / seatsPerRow; // exact — no remainder ever
                        int endRow = currentRow + rows - 1;

                        var section = new SeatSection
                        {
                            AircraftId = aircraft.Id,
                            TravelClass = travelClass,
                            StartRow = currentRow,
                            EndRow = endRow,
                            Layout = layout
                        };

                        context.SeatSections.Add(section);

                        // simple nested loop — no done flag needed, rows are exact
                        for (int row = currentRow; row <= endRow; row++)
                        {
                            foreach (var block in layout.Split('-'))
                            {
                                foreach (char letter in block)
                                {
                                    context.Seats.Add(new Seat
                                    {
                                        SeatSectionId = section.Id,
                                        TravelClass = travelClass,
                                        SeatNumber = $"{row}{letter}"
                                    });
                                }
                            }
                        }

                        currentRow = endRow + 1;
                    }

                    CreateSection(TravelClass.First, firstSeats, "AB-CD");
                    CreateSection(TravelClass.Business, businessSeats, "ABC-DEF");
                    CreateSection(TravelClass.Economy, economySeats, "ABC-DEF");
                }

                await context.SaveChangesAsync();

                for (int i = 0; i < 20; i++)
                {
                    Airport dep, arr;

                    dep = random.Next(1, 101) <= 70
                        ? priorityAirports[random.Next(priorityAirports.Count)]
                        : airports[random.Next(airports.Count)];

                    do
                    {
                        arr = random.Next(1, 101) <= 70
                            ? priorityAirports[random.Next(priorityAirports.Count)]
                            : airports[random.Next(airports.Count)];
                    }
                    while (arr.Id == dep.Id);

                    var ac = aircraftList[random.Next(aircraftList.Count)];
                    var duration = random.Next(60, 240);
                    var departs = DateTime.UtcNow
                        .AddDays(random.Next(1, 20))
                        .AddHours(random.Next(0, 24))
                        .AddMinutes(random.Next(0, 60));

                    context.Flights.Add(new Flight
                    {
                        FlightNumber = $"{company.IATA}{random.Next(100, 9999)}",
                        DepartureAirportId = dep.Id,
                        ArrivalAirportId = arr.Id,
                        DepartureTime = departs,
                        ArrivalTime = departs.AddMinutes(duration),
                        DurationMinutes = duration,
                        Price = random.Next(40, 450),
                        AirlineId = airline.Id,
                        AircraftId = ac.Id
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}