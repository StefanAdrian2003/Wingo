namespace Proiect_Licenta.Models
{
    public class ConnectingFlight
    {
        public Flight Leg1 { get; set; }
        public Flight Leg2 { get; set; }

        public decimal TotalPrice => Leg1.Price + Leg2.Price;

        public TimeSpan LayoverDuration => Leg2.DepartureTime - Leg1.ArrivalTime;
    }
}