namespace Proiect_Licenta.Models
{
    public class FlightSeat : BaseObject
    {
        public Guid FlightId { get; set; }
        public Flight Flight { get; set; }

        public Guid SeatId { get; set; }
        public Seat Seat { get; set; }

        public Guid? TicketId { get; set; }
        public Ticket? Ticket { get; set; }
    }
}
