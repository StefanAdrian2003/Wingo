namespace Proiect_Licenta.Models
{
    public enum NotificationType
    {
        Like,
        Comment,
        Badge,
        Booking,
        FlightCancelled,
        PostDeleted,
        CommentDeleted,
        ReportRejected,
        ReportApproved
    }

    public class Notification : BaseObject
    {
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public string Message { get; set; }
        public Guid? PostId { get; set; }
        public Post? Post { get; set; } 

        // cine primește notificarea
        public string UserId { get; set; }  //BUN
        public User User { get; set; }  //BUN

        public string? SenderId { get; set; }
        public User? Sender { get; set; }
    }
}