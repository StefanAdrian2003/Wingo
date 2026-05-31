namespace Proiect_Licenta.Models
{
    public class UserBadge : BaseObject
    {
        public required string UserId { get; set; }  // FK → User  //BUN
        public User User { get; set; }  //BUN


        public required Guid BadgeId { get; set; }   // FK → Badge  //BUN
        public Badge Badge { get; set; }  //BUN
    }
}
