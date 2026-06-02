namespace Proiect_Licenta.Models
{
    public class BaggageSelectionDto
    {
        public Guid SeatId { get; set; }
        public string SeatNumber { get; set; } = "";
        public string BaggageType { get; set; } = "None"; // None | Cabin | Checked20 | Checked32
        public bool HasExtraBag { get; set; } = false;
        public decimal TotalBaggagePrice { get; set; }
    }
}
