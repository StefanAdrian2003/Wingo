namespace Proiect_Licenta.Models
{
    public class RoundTrip
    {
        public Flight OutboundFlight { get; set; }
        public Flight ReturnFlight { get; set; }

        public decimal TotalPrice => (OutboundFlight.Price + ReturnFlight.Price) * 0.90M;
    }
}
