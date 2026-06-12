using Microsoft.EntityFrameworkCore;

namespace Proiect_Licenta.Models
{

    public class Ticket : BaseObject
    {
        
        [Precision(18, 2)]
        public decimal Price { get; set; }
        public TravelClass TravelClass { get; set; }

        public Guid BookingId { get; set; }
        public Booking Booking { get; set; }

        public FlightSeat FlightSeat { get; set; }

        public ICollection<BaggageItem> BaggageItems { get; set; } = new List<BaggageItem>();

    }
}