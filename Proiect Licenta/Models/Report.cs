namespace Proiect_Licenta.Models
{
    public enum ReportStatus
    {
        Pending,
        Reviewed,
        Resolved,
        Rejected
    }

    public enum ReportType
    {
        Post,
        Comment,
        Flight
    }

    public class Report : BaseObject
    {
        

        // ce tip de report este
        public ReportType Type { get; set; }

        // motivul
        public string Reason { get; set; }

        // status report
        public ReportStatus Status { get; set; } = ReportStatus.Pending;


        // cine raporteaza
        public string ReporterId { get; set; }  //BUN
        public User Reporter { get; set; }  //BUN
        public Guid? PostId { get; set; }  //BUN
        public Post? Post { get; set; }  //BUN

        public Guid? CommentId { get; set; }  //BUN
        public Comment? Comment { get; set; }  //BUN

        public Guid? FlightId { get; set; }  //BUN
        public Flight? Flight { get; set; }  //BUN

        // admin review
        public string? ReviewedByAdminId { get; set; }  //BUN
        public User? ReviewedByAdmin { get; set; }  //BUN
    }
}