using Proiect_Licenta.Pages;

namespace Proiect_Licenta.Models
{
    public class BookingSessionDto
    {
        public Guid FlightId { get; set; }
        public List<Guid> SelectedSeatIds { get; set; } = new();
        public List<BaggageSelectionDto> Baggage { get; set; } = new();
    }
}
