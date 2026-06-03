namespace Proiect_Licenta.Models
{
    public class RoundTripConnecting
    {
        public ConnectingFlight OutboundJourney { get; set; }
        public ConnectingFlight ReturnJourney { get; set; }

        // Because this is technically a Round Trip, we bring back the 10% discount!
        public decimal TotalPrice => (OutboundJourney.TotalPrice + ReturnJourney.TotalPrice) * 0.90M;
    }
}