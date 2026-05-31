namespace Proiect_Licenta.Models
{
    public class Aircraft : BaseObject
    {
        public required string Model { get; set; }

        public Guid AirlineId { get; set; }
        public Airline Airline { get; set; }
        public int TotalSeats { get; set; }
        public ICollection<Flight> Flights { get; set; } = new List<Flight>();
        public ICollection<SeatSection> SeatSections { get; set; } = new List<SeatSection>();
    }
}