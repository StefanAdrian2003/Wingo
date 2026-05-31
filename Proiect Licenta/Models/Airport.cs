namespace Proiect_Licenta.Models
{
    public class Airport : BaseObject
    {
        public required string Name { get; set; }

        public required string City { get; set; }

        public required string Country { get; set; }

        public required string IATACode { get; set; }
        public required string ICAOCode { get; set; }
        public ICollection<Flight> DepartingFlights { get; set; } = new List<Flight>();  //BUN

        public ICollection<Flight> ArrivingFlights { get; set; } = new List<Flight>();  //BUN

    }
}
