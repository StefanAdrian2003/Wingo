using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Proiect_Licenta.Models
{
    public class Flight : BaseObject
    {
        public required string FlightNumber { get; set; }

        public int DurationMinutes { get; set; }

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        [Precision(18, 2)]
        public decimal Price { get; set; }

        public Guid DepartureAirportId { get; set; }  //BUN
        public Airport DepartureAirport { get; set; }  //BUN

        public Guid ArrivalAirportId { get; set; }  //BUN
        public Airport ArrivalAirport { get; set; }  //BUN



        [Required]
        public Guid AirlineId { get; set; }  //BUN
        public Airline Airline { get; set; }  //BUN

        [Required]
        public Guid AircraftId { get; set; }  //BUN
        public Aircraft Aircraft { get; set; }  //BUN

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();  //BUN
        public ICollection<Report> Reports { get; set; } = new List<Report>();  //BUN
        public ICollection<FlightSeat> FlightSeats { get; set; } = new List<FlightSeat>();

    }
}
