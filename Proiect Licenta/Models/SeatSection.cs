using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Models
{
    public class SeatSection : BaseObject
    {
        public Guid AircraftId { get; set; }
        public Aircraft Aircraft { get; set; }

        public TravelClass TravelClass { get; set; }

        // inclusive row range (e.g. 1–5 First Class)
        public int StartRow { get; set; }
        public int EndRow { get; set; }

        // layout per row, example:
        // "AB-CD" = 2 seats + aisle + 2 seats
        // "ABC-DEF" = 3 seats + aisle + 3 seats
        public string Layout { get; set; } = string.Empty;

        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}