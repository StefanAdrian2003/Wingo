namespace Proiect_Licenta.Models
{
    public class Reservation : BaseObject
    {
        public string UserId { get; set; }
        public User User { get; set; }
        public List<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
