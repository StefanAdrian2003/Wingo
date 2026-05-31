

namespace Proiect_Licenta.Models
{
    public class CreateFlight
    {
        public string FlightNumber { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public Guid AircraftId { get; set; }

    }
}
