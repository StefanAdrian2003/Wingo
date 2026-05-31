using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Models
{
    public class Badge : BaseObject
    {
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public required string Icon { get; set; }
        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();  //BUN
    }
}
