namespace Proiect_Licenta.Models
{
    public class Booking : BaseObject
    {
        public required string UserId { get; set; }
        public User User { get; set; }

        public Guid FlightId { get; set; }
        public Flight Flight { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}