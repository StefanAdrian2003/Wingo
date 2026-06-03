namespace Proiect_Licenta.Models
{
    public class BookingSessionDto
    {
        public Guid FlightId { get; set; }
        public List<Guid> SelectedSeatIds { get; set; } = new();

        public Guid? ReturnFlightId { get; set; }
        public List<Guid> ReturnSeatIds { get; set; } = new();

        public bool IsRoundTrip => ReturnFlightId.HasValue;

        public List<BaggageSelectionDto> Baggage { get; set; } = new();
        public List<BaggageSelectionDto> ReturnBaggage { get; set; } = new();

        public List<string> ConflictedSeats { get; set; } = new();

        public List<string> ConflictedSeatsReturn { get; set; } = new();
    }
}