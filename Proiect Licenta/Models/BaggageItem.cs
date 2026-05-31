using Microsoft.EntityFrameworkCore;

namespace Proiect_Licenta.Models
{
    public enum BaggageType
    {
        Cabin,
        Checked,
        Extra
    }

    public class BaggageItem : BaseObject
    {
        public Guid TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public BaggageType Type { get; set; }

        public int? WeightKg { get; set; }

        [Precision(18, 2)]
        public decimal Price { get; set; }
    }
}