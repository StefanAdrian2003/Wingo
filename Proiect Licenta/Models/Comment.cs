using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Models
{
    public class Comment : BaseObject
    {
        [Required]
        public required string Content { get; set; }

        public required string UserId { get; set; }  //BUN
        public required User User { get; set; }  //BUN

        public required Guid PostId { get; set; }  //BUN
        public required Post Post { get; set; }  //BUN

        public ICollection<Report> Reports { get; set; } = new List<Report>();  //BUN
    }
}
