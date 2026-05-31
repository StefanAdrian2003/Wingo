using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Models
{
    public class Post : BaseObject
    {

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }
        [StringLength(2000)]
        public string? Description { get; set; }
        [Required]
        public required string ImagePath { get; set; }

        public required string UserId { get; set; }     //BUN
        public required User User { get; set; }   //BUN

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();  //BUN
        public ICollection<Like> Likes { get; set; } = new List<Like>();  //BUN
        public ICollection<Report> Reports { get; set; } = new List<Report>();  //BUN
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();  //BUN
    }
}
