namespace Proiect_Licenta.Models
{
    public class Seat : BaseObject
    {
        public Guid SeatSectionId { get; set; }
        public SeatSection SeatSection { get; set; }

        public TravelClass TravelClass { get; set; }

        public string SeatNumber { get; set; } = string.Empty; // "12A"
        public ICollection<FlightSeat> FlightSeats { get; set; } = new List<FlightSeat>();
    }
}