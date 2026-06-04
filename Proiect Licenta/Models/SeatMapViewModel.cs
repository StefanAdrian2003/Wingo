using Proiect_Licenta.Models;

namespace Proiect_Licenta.Models
{
    public class SeatMapViewModel
    {
        public Flight Flight { get; set; } = null!;
        public Guid FlightId { get; set; }
        public decimal BasePrice { get; set; }
        public Dictionary<TravelClass, decimal> Multipliers { get; set; } = new();
        public List<Guid> Preselected { get; set; } = new();

        /// <summary>data-flight attribute value, e.g. "outbound", "leg2", "return", "retLeg2"</summary>
        public string DataFlight { get; set; } = "outbound";

        /// <summary>CSS class for available seats, e.g. "available", "leg2-available", "return-available"</summary>
        public string CssAvailable { get; set; } = "available";
    }
}