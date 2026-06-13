namespace Proiect_Licenta.Models
{

    public enum BookingStep
    {
        FlightSelection,
        PassengerDetails,
        BaggageSelection,
        ReviewPage,
        PaymentPage
    }
    public class BookingSessionDto
    {
        public Guid FlightId { get; set; }
        public List<Guid> SelectedSeatIds { get; set; } = new();

        // Leg2 of outbound (only for layover)
        public Guid? Leg2FlightId { get; set; }
        public List<Guid> Leg2SeatIds { get; set; } = new();

        public Guid? ReturnFlightId { get; set; }
        public List<Guid> ReturnSeatIds { get; set; } = new();

        // Leg2 of return (only for RoundTripConnecting)
        public Guid? ReturnLeg2FlightId { get; set; }
        public List<Guid> ReturnLeg2SeatIds { get; set; } = new();

        public bool IsRoundTrip => ReturnFlightId.HasValue;
        public bool IsLayover => Leg2FlightId.HasValue;
        public BookingStep HighestAllowedStep { get; set; } = BookingStep.FlightSelection;

        public List<BaggageSelectionDto> Baggage { get; set; } = new();
        public List<BaggageSelectionDto> ReturnBaggage { get; set; } = new();
        public List<BaggageSelectionDto> Leg2Baggage { get; set; } = new();
        public List<BaggageSelectionDto> ReturnLeg2Baggage { get; set; } = new();

        public List<string> ConflictedSeats { get; set; } = new();

        public List<string> ConflictedLeg2Seats { get; set; } = new();

        public List<string> ConflictedSeatsReturn { get; set; } = new();

        public List<string> ConflictedReturnLeg2Seats { get; set; } = new();
    }
}