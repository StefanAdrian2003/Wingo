using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Models
{
    public class BaseObject
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime DateOfCreation { get; set; } = DateTime.UtcNow;
    }
}
