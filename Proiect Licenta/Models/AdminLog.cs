namespace Proiect_Licenta.Models
{
    public class AdminLog : BaseObject
    {
        public string Action { get; set; }

        public string? PerformedByUserId { get; set; }
        public User? PerformedByUser { get; set; }

        public string Details { get; set; }
    }
}
