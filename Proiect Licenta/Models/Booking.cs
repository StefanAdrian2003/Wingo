namespace Proiect_Licenta.Models
{
    public class Booking : BaseObject
    {
        public required Guid ReservationId { get; set; }
        public Reservation Reservation { get; set; }

        public Guid FlightId { get; set; }
        public Flight Flight { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}