using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<AdminLog> AdminLogs { get; set; }
        public DbSet<Aircraft> Aircrafts { get; set; }
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<BaggageItem> BaggageItems { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<FlightSeat> FlightSeats { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<SeatSection> SeatSections { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }

        


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ------------------------------
            // LIKE
            // ------------------------------

            // Like → Post (postul poate fi șters fără să ștergă like-urile)
            builder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.NoAction);

            // Like → User (ștergere User șterge like-urile)
            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Like unic pe User + Post
            builder.Entity<Like>()
               .HasIndex(l => new { l.UserId, l.PostId })
               .IsUnique();

            // ------------------------------
            // COMMENT
            // ------------------------------

            // Comment → Post (postul poate fi șters fără să ștergă comentariile)
            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.NoAction);

            // Comment → User (ștergere User șterge comentariile)
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ------------------------------
            // USERBADGE
            // ------------------------------

            // UserBadge → Badge (ștergere Badge șterge UserBadge)
            builder.Entity<UserBadge>()
                .HasOne(ub => ub.Badge)
                .WithMany(b => b.UserBadges)
                .HasForeignKey(ub => ub.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserBadge → User (ștergere User șterge UserBadge)
            builder.Entity<UserBadge>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ------------------------------
            // VOUCHER
            // ------------------------------

            // Voucher → User (opțional)
            builder.Entity<Voucher>()
                .HasOne(v => v.User)
                .WithMany(u => u.Vouchers)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Voucher.Code trebuie unic
            builder.Entity<Voucher>()
                .HasIndex(v => v.Code)
                .IsUnique();

            // ------------------------------
            // POST
            // ------------------------------

            // Post → User (creatorul postării)
            builder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ------------------------------
            // AIRCRAFT
            // ------------------------------

            // Aircraft → Airline (fiecare avion aparține unei companii)
            builder.Entity<Aircraft>()
                .HasOne(a => a.Airline)
                .WithMany(al => al.Aircraft)
                .HasForeignKey(a => a.AirlineId)
                .OnDelete(DeleteBehavior.Cascade);

            // ------------------------------
            // FLIGHT
            // ------------------------------

            // Flight → Airline (zborul aparține unei companii)
            builder.Entity<Flight>()
                .HasOne(f => f.Airline)
                .WithMany(al => al.Flights)
                .HasForeignKey(f => f.AirlineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Flight → Aircraft (zborul folosește un avion)
            builder.Entity<Flight>()
                .HasOne(f => f.Aircraft)
                .WithMany(a => a.Flights)
                .HasForeignKey(f => f.AircraftId)
                .OnDelete(DeleteBehavior.Restrict); // nu șterge avionul dacă are zboruri active

            // ------------------------------
            // BOOKING
            // ------------------------------

            // Reservation → Bookings (Deleting a Reservation automatically wipes out all 4 sub-bookings/legs)
            builder.Entity<Booking>()
                .HasOne(b => b.Reservation)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking → Flight (Keep Restrict to prevent deleting an entire Flight from deleting user records)
            builder.Entity<Booking>()
                .HasOne(b => b.Flight)
                .WithMany(f => f.Bookings)
                .HasForeignKey(b => b.FlightId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → Flight (pentru ce zbor)
            builder.Entity<Booking>()
                .HasOne(b => b.Flight)
                .WithMany(f => f.Bookings)
                .HasForeignKey(b => b.FlightId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Reservations (Deleting a user wipes out their entire reservation history)
            builder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ------------------------------
            // AIRLINE → USER (un user poate fi asociat cu o companie)
            // ------------------------------

            builder.Entity<Airline>()
                .HasOne(al => al.User)
                .WithOne(u => u.Airline)
                .HasForeignKey<Airline>(al => al.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Airline>()
                .HasIndex(a => a.UserId)
                .IsUnique();

            // index unic pentru a preveni duplicarea companiilor
            builder.Entity<Airline>()
                .HasIndex(a => new { a.Name, a.IATACode, a.Country })
                .IsUnique();


            builder.Entity<Flight>()
                .HasOne(f => f.DepartureAirport)
                .WithMany(a => a.DepartingFlights)
                .HasForeignKey(f => f.DepartureAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Flight>()
                .HasOne(f => f.ArrivalAirport)
                .WithMany(a => a.ArrivingFlights)
                .HasForeignKey(f => f.ArrivalAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================================================
            // NOTIFICATIONS
            // ============================================================================

            // Utilizatorul care primește notificarea (Dacă userul se șterge, se șterg și notificările lui)
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Utilizatorul care a provocat notificarea (opțional)
            builder.Entity<Notification>()
                .HasOne(n => n.Sender)
                .WithMany() // Nu avem nevoie de colecție inversă în User
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Previne cascade loops

            // Postarea legată de notificare (opțional)
            builder.Entity<Notification>()
                .HasOne(n => n.Post)
                .WithMany(p => p.Notifications)    // ← claims the collection
                .HasForeignKey(n => n.PostId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================================================
            // REPORTS
            // ============================================================================

            // Cine trimite raportul
            builder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.Reports) // Se leagă de singura colecție generică de Reports din clasa User
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);


            // Adminul care verifică raportul (opțional)
            builder.Entity<Report>()
                .HasOne(r => r.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // Postarea raportată (opțional)
            builder.Entity<Report>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Reports) // S-a mapat pe colecția din Post.cs
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comentariul raportat (opțional)
            builder.Entity<Report>()
                .HasOne(r => r.Comment)
                .WithMany(c => c.Reports) // S-a mapat pe colecția din Comment.cs
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Zborul raportat (opțional)
            builder.Entity<Report>()
                .HasOne(r => r.Flight)
                .WithMany(f => f.Reports) // S-a mapat pe colecția din Flight.cs
                .HasForeignKey(r => r.FlightId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================================================
            // ADMIN LOGS (Adăugat extra, fiindcă lipsea complet)
            // ============================================================================
            builder.Entity<AdminLog>()
                .HasOne(al => al.PerformedByUser)
                .WithMany() // Nu avem nevoie de o listă uriașă de loguri în interiorul obiectului User
                .HasForeignKey(al => al.PerformedByUserId)
                .OnDelete(DeleteBehavior.SetNull); // Dacă userul e șters, logul rămâne, dar PerformedByUserId devine NULL


            builder.Entity<SeatSection>()
                .HasOne(s => s.Aircraft)
                .WithMany(a => a.SeatSections)
                .HasForeignKey(s => s.AircraftId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Seat>()
                .HasOne(s => s.SeatSection)
                .WithMany(ss => ss.Seats)
                .HasForeignKey(s => s.SeatSectionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FlightSeat>()
                .HasOne(fs => fs.Flight)
                .WithMany(f => f.FlightSeats)
                .HasForeignKey(fs => fs.FlightId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FlightSeat>()
                .HasOne(fs => fs.Seat)
                .WithMany(s => s.FlightSeats)
                .HasForeignKey(fs => fs.SeatId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<FlightSeat>()
                .HasIndex(fs => new { fs.FlightId, fs.SeatId })
                .IsUnique();

            builder.Entity<FlightSeat>()
                .HasOne(fs => fs.Ticket)
                .WithOne(t => t.FlightSeat)          // ← claims the navigation
                .HasForeignKey<FlightSeat>(fs => fs.TicketId)
                .OnDelete(DeleteBehavior.NoAction);


            builder.Entity<Ticket>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.Tickets)
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<Badge>().HasData(

                // ---------BADGE-URI POSTARI------------
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000000"),
                    Name = "Welcome",
                    Description = "You have created your account!", // badge pentru toți userii
                    Icon = "/images/welcome.png"
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Name = "First Post",
                    Description = "You posted for the first time",
                    Icon = "/images/post1.png" // dacă vrei emoji
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                    Name = "Traveler",
                    Description = "You made 5 posts",
                    Icon = "/images/post5.png"
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000020"),
                    Name = "Explorer",
                    Description = "You made 20 posts",
                    Icon = "/images/post20.png"
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000050"),
                    Name = "Adventurer",
                    Description = "You made 50 posts",
                    Icon = "/images/post50.png"
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000100"),
                    Name = "Storyteller",
                    Description = "You made 100 posts",
                    Icon = "/images/post100.png"
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000250"),
                    Name = "Content Creator",
                    Description = "You made 250 posts",
                    Icon = "/images/post250.png"
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000500"),
                    Name = "Master Explorer",
                    Description = "You made 500 posts",
                    Icon = "/images/post500.png"
                },
                new Badge
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000001000"),
                    Name = "Legendary Poster",
                    Description = "You made 1,000 posts",
                    Icon = "/images/post1000.png"
                },



                // ---------BADGE-URI LEVEL------------
                new Badge
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000005"),
                    Name = "Getting Started",
                    Description = "Reached level 5",
                    Icon = "/images/level5.png"
                },
                new Badge
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000010"),
                    Name = "Rising Star",
                    Description = "Reached level 10",
                    Icon = "/images/level10.png"
                },
                new Badge
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000020"),
                    Name = "Challenger",
                    Description = "Reached level 20",
                    Icon = "/images/level20.png"
                },
                new Badge
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000050"),
                    Name = "Veteran Explorer",
                    Description = "Reached level 50",
                    Icon = "/images/level50.png"
                },
                new Badge
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000100"),
                    Name = "Legend of Wingo",
                    Description = "Reached level 100",
                    Icon = "/images/level100.png"
                },




                // ---------BADGE-URI LIKE-URI------------
                new Badge
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    Name = "First Love",
                    Description = "Gave your first like",
                    Icon = "/images/like1.png"
                },
                new Badge
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000010"),
                    Name = "Supporter",
                    Description = "Gave 10 likes",
                    Icon = "/images/like10.png"
                },
                new Badge
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000050"),
                    Name = "Positive Vibes",
                    Description = "Gave 50 likes",
                    Icon = "/images/like50.png"
                },
                new Badge
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000100"),
                    Name = "Community Booster",
                    Description = "Gave 100 likes",
                    Icon = "/images/like100.png"
                },
                new Badge
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000500"),
                    Name = "Influencer",
                    Description = "Gave 500 likes",
                    Icon = "/images/like500.png"
                },
                new Badge
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000001000"),
                    Name = "Social Machine",
                    Description = "Gave 1,000 likes",
                    Icon = "/images/like1000.png"
                },
                new Badge
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000005000"),
                    Name = "Infinity Reactor",
                    Description = "Gave 5,000 likes",
                    Icon = "/images/like5000.png"
                },


                // ---------BADGE-URI COMENTARII------------
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                    Name = "First Words",
                    Description = "Posted your first comment",
                    Icon = "/images/comment1.png"
                },
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000005"),
                    Name = "Conversationalist",
                    Description = "Posted 5 comments",
                    Icon = "/images/comment5.png"
                },
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000015"),
                    Name = "Active Voice",
                    Description = "Posted 15 comments",
                    Icon = "/images/comment15.png"
                },
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000050"),
                    Name = "Discussion Leader",
                    Description = "Posted 50 comments",
                    Icon = "/images/comment50.png"
                },
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000100"),
                    Name = "Community Speaker",
                    Description = "Posted 100 comments",
                    Icon = "/images/comment100.png"
                },
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000250"),
                    Name = "Debater Pro",
                    Description = "Posted 250 comments",
                    Icon = "/images/comment250.png"
                },
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000500"),
                    Name = "Social Anchor",
                    Description = "Posted 500 comments",
                    Icon = "/images/comment500.png"
                },
                new Badge
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000001000"),
                    Name = "Voice of Wingo",
                    Description = "Posted 1,000 comments",
                    Icon = "/images/comment1000.png"
                }

            );
        }

    }
}
