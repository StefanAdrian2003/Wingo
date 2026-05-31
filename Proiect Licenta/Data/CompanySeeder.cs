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
            if (await context.Users.AnyAsync(u => u.IsCompany))
                return;

            var random = new Random();

            // ---------------- AIRPORTS ----------------

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

            // aeroporturi foarte folosite
            var priorityAirports = airports
                .Where(a =>
                    a.IATACode == "OTP" ||
                    a.IATACode == "CLJ" ||
                    a.IATACode == "IAS" ||
                    a.IATACode == "TSR" ||
                    a.IATACode == "LTN" ||
                    a.IATACode == "CDG" ||
                    a.IATACode == "FCO" ||
                    a.IATACode == "AMS" ||
                    a.IATACode == "IST" ||
                    a.IATACode == "MAD")
                .ToList();

            // ---------------- COMPANIES ----------------

            var companies = new List<(string Name, string IATA, string Country)>
            {
                ("Lufthansa", "LH", "Germany"),
                ("Air France", "AF", "France"),
                ("KLM", "KL", "Netherlands"),
                ("Emirates", "EK", "UAE"),
                ("Qatar Airways", "QR", "Qatar"),
                ("Turkish Airlines", "TK", "Turkey"),
                ("Ryanair", "FR", "Ireland"),
                ("Wizz Air", "W6", "Hungary"),
                ("British Airways", "BA", "United Kingdom"),
                ("ITA Airways", "AZ", "Italy"),
                ("Tarom", "RO", "Romania"),
                ("Aegean Airlines", "A3", "Greece"),
                ("Austrian Airlines", "OS", "Austria"),
                ("Swiss", "LX", "Switzerland"),
                ("Iberia", "IB", "Spain"),
                ("easyJet", "U2", "United Kingdom"),
                ("Finnair", "AY", "Finland"),
                ("LOT Polish Airlines", "LO", "Poland"),
                ("SAS", "SK", "Sweden"),
                ("Brussels Airlines", "SN", "Belgium")
            };

            string[] aircraftModels =
            {
                "Airbus A320",
                "Boeing 737",
                "Airbus A321",
                "Boeing 777",
                "Embraer E190",
                "Airbus A330",
                "ATR 72",
                "Boeing 787 Dreamliner"
            };

            foreach (var company in companies)
            {
                var email =
                    $"{company.Name.Replace(" ", "").ToLower()}@wingo.com";

                var existingUser =
                    await userManager.FindByEmailAsync(email);

                if (existingUser != null)
                    continue;

                // ---------------- USER ----------------

                var user = new User
                {
                    FirstName = company.Name,
                    LastName = "Official",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    IsCompany = true
                };

                var result =
                    await userManager.CreateAsync(user, "Company123!");

                if (!result.Succeeded)
                    continue;

                await userManager.AddToRoleAsync(user, "Company");

                // ---------------- AIRLINE ----------------

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

                // ---------------- AIRCRAFT ----------------

                var aircraftList = new List<Aircraft>();

                for (int i = 0; i < 5; i++)
                {
                    int rawSeats = random.Next(10, 27); // 10*12=120, 26*12=312
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


                // ---------------- SEAT STRUCTURE + SEATS ----------------

                foreach (var aircraft in aircraftList)
                {
                    int totalSeats = aircraft.TotalSeats;

                    // împărțire realistă
                    int firstSeats = (int)(totalSeats * 0.1);   // 10%
                    int businessSeats = (int)(totalSeats * 0.2); // 20%
                    int economySeats = totalSeats - firstSeats - businessSeats;

                    int currentRow = 1;

                    void CreateSection(TravelClass travelClass, int seatCount, string layout)
                    {
                        if (seatCount <= 0) return;

                        int seatsPerRow = layout.Replace("-", "").Length;

                        int rows = (int)Math.Ceiling((double)seatCount / seatsPerRow);
                        int endRow = currentRow + rows - 1;

                        var section = new SeatSection
                        {
                            AircraftId = aircraft.Id,
                            Aircraft = aircraft,
                            TravelClass = travelClass,
                            StartRow = currentRow,
                            EndRow = endRow,
                            Layout = layout
                        };

                        context.SeatSections.Add(section);
                        aircraft.SeatSections.Add(section);

                        int seatIndex = 0;

                        for (int row = currentRow; row <= endRow; row++)
                        {
                            foreach (var block in layout.Split('-'))
                            {
                                foreach (char seatLetter in block)
                                {
                                    if (seatIndex >= seatCount)
                                        break;

                                    var seat = new Seat
                                    {
                                        SeatSection = section,
                                        SeatSectionId = section.Id,
                                        TravelClass = travelClass,
                                        SeatNumber = $"{row}{seatLetter}"
                                    };

                                    context.Seats.Add(seat);
                                    section.Seats.Add(seat);

                                    seatIndex++;
                                }
                            }
                        }

                        currentRow = endRow + 1;
                    }

                    // FIRST CLASS
                    CreateSection(TravelClass.First, firstSeats, "AB-CD");

                    // BUSINESS
                    CreateSection(TravelClass.Business, businessSeats, "ABC-DEF");

                    // ECONOMY
                    CreateSection(TravelClass.Economy, economySeats, "ABC-DEF");
                }

                await context.SaveChangesAsync();

                // ---------------- FLIGHTS ----------------

                for (int i = 0; i < 20; i++)
                {
                    Airport departureAirport;
                    Airport arrivalAirport;

                    // 70% sanse aeroport popular
                    if (random.Next(1, 101) <= 70)
                    {
                        departureAirport =
                            priorityAirports[random.Next(priorityAirports.Count)];
                    }
                    else
                    {
                        departureAirport =
                            airports[random.Next(airports.Count)];
                    }

                    do
                    {
                        if (random.Next(1, 101) <= 70)
                        {
                            arrivalAirport =
                                priorityAirports[random.Next(priorityAirports.Count)];
                        }
                        else
                        {
                            arrivalAirport =
                                airports[random.Next(airports.Count)];
                        }
                    }
                    while (arrivalAirport.Id == departureAirport.Id);

                    var aircraft =
                        aircraftList[random.Next(aircraftList.Count)];

                    // zboruri mai apropiate pentru teste
                    var departureTime =
                        DateTime.UtcNow
                            .AddDays(random.Next(1, 20))
                            .AddHours(random.Next(0, 24))
                            .AddMinutes(random.Next(0, 60));

                    var duration =
                        random.Next(60, 240);

                    var flight = new Flight
                    {
                        FlightNumber =
                            $"{company.IATA}{random.Next(100, 9999)}",

                        DepartureAirportId = departureAirport.Id,
                        ArrivalAirportId = arrivalAirport.Id,

                        DepartureTime = departureTime,

                        ArrivalTime =
                            departureTime.AddMinutes(duration),

                        DurationMinutes = duration,

                        Price =
                            random.Next(40, 450),

                        AirlineId = airline.Id,
                        Airline = airline,

                        AircraftId = aircraft.Id,
                        Aircraft = aircraft
                    };

                    context.Flights.Add(flight);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}