namespace Proiect_Licenta.Models
{
    public class Like : BaseObject
    {
        public required string UserId { get; set; }   //BUN
        public required User User { get; set; }  //BUN

        public required Guid PostId { get; set; }     //BUN
        public required Post Post { get; set; }  //BUN
    }
}
