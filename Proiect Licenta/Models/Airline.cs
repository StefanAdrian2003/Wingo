namespace Proiect_Licenta.Models
{
    public class Airline : BaseObject
    {
        public required string Name { get; set; }
        public required string IATACode { get; set; }
        public required string Country { get; set; }
        public required string LogoUrl { get; set; }
        public required string UserId { get; set; }  //BUN
        public User User { get; set; }  //BUN
        public ICollection<Aircraft> Aircraft { get; set; } = new List<Aircraft>();  //BUN
        public ICollection<Flight> Flights { get; set; } = new List<Flight>();  //BUN
    }
}
