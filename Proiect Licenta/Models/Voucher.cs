using Microsoft.EntityFrameworkCore;

namespace Proiect_Licenta.Models
{
    public class Voucher
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        // FK către User
        public string? UserId { get; set; }  //BUN
        public User? User { get; set; }  //BUN

        public string Code { get; set; } = "";
    }
}
