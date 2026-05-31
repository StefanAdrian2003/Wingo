using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public required string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public required string LastName { get; set; }
        public int? Level { get; set; } = 1;
        public int? TotalPoints { get; set; } = 0;
        public int FlightsBooked { get; set; } = 0;
        public string? ProfilePictureUrl { get; set; }
        public DateTime? LastLevelUp { get; set; }
        public bool IsCompany { get; set; }
        public Guid? AirlineId { get; set; }  //BUN
        public Airline? Airline { get; set; }  //BUN
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();  //BUN
        public ICollection<Post> Posts { get; set; } = new List<Post>();  //BUN
        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();  //BUN
        public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();  //BUN
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();  //BUN
        public ICollection<Like> Likes { get; set; } = new List<Like>();  //BUN
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();  //BUN
        public ICollection<Report> Reports { get; set; } = new List<Report>();  //BUN

    }
}
