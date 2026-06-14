using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;
using Proiect_Licenta.Services;

namespace Proiect_Licenta.Pages
{
    [EnableRateLimiting("fixed")]
    public class _ActionsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly UserProgressService _userProgressService;
        private readonly BadgeService _badgeService;
        private readonly NotificationService _notificationService;
        private readonly IWebHostEnvironment _environment;

        public _ActionsModel(
            ApplicationDbContext db,
            UserManager<User> userManager,
            UserProgressService userProgressService,
            BadgeService badgeService,
            NotificationService notificationService,
            IWebHostEnvironment environment)
        {
            _db = db;
            _userManager = userManager;
            _userProgressService = userProgressService;
            _badgeService = badgeService;
            _notificationService = notificationService;
            _environment = environment;
        }

        private const int POINTS_FOR_NEW_LIKE = 5;
        private const int POINTS_FOR_NEW_COMMENT = 10;

        public async Task<IActionResult> OnPostLikeAsync(Guid postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return new JsonResult(new { error = "Not logged in" }) { StatusCode = 401 };

            var existingLike = await _db.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == user.Id);

            if (existingLike != null)
            {
                _db.Likes.Remove(existingLike);
                await _userProgressService.AddPointsAsync(user, -POINTS_FOR_NEW_LIKE);
            }
            else
            {
                var post = await _db.Posts.FindAsync(postId);
                if (post == null) return NotFound();

                var like = new Like
                {
                    UserId = user.Id,
                    PostId = postId,
                    User = user,
                    Post = post
                };
                _db.Likes.Add(like);

                var recentLikeLog = await _db.AdminLogs
                    .AnyAsync(l =>
                        l.PerformedByUserId == user.Id &&
                        l.Action == "LIKE_POST" &&
                        l.DateOfCreation > DateTime.UtcNow.AddMinutes(-5));

                if (!recentLikeLog)
                {
                    _db.AdminLogs.Add(new AdminLog
                    {
                        Action = "LIKE_POST",
                        PerformedByUserId = user.Id,
                        Details = $"{user.UserName} liked post {post.Id}"
                    });
                }

                var alreadyNotified = await _db.Notifications
                    .AnyAsync(n =>
                        n.PostId == post.Id &&
                        n.SenderId == user.Id &&
                        n.Type == NotificationType.Like);

                if (!alreadyNotified && post.UserId != user.Id)
                {
                    await _notificationService.CreateAsync(
                        receiverId: post.UserId,
                        senderId: user.Id,
                        type: NotificationType.Like,
                        message: $"{user.UserName} liked your post.",
                        postId: post.Id
                    );
                }

                await _userProgressService.AddPointsAsync(user, POINTS_FOR_NEW_LIKE);
                var userId = user.Id;
                await _badgeService.CheckPostingBadgesAsync(userId);
            }

            await _db.SaveChangesAsync();

            var likeCount = await _db.Likes.CountAsync(l => l.PostId == postId);
            return Content($"<span id=\"like-count-{postId}\">{likeCount}</span>", "text/html");
        }

        public async Task<IActionResult> OnPostCommentAsync(Guid postId, string content, [FromServices] CommentModerationService moderation)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            bool isSafe = await moderation.IsSafeAsync(content);

            if (!isSafe)
            {
                TempData["CommentError"] = "Comment rejected by AI";

                var postWithComments = await _db.Posts
                    .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(p => p.Id == postId);

                return Partial("_CommentsList", postWithComments);
            }

            var post = await _db.Posts
                .Include(p => p.Comments)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound("Post not found");

            var comment = new Comment
            {
                Content = content,
                UserId = user.Id,
                PostId = post.Id,
                User = user,
                Post = post
            };

            _db.Comments.Add(comment);

            _db.AdminLogs.Add(new AdminLog
            {
                Action = "COMMENT_POST",
                PerformedByUserId = user.Id,
                Details = $"{user.UserName} commented on post {post.Id}"
            });

            if (post.UserId != user.Id)
            {
                await _notificationService.CreateAsync(
                    receiverId: post.UserId,
                    senderId: user.Id,
                    type: NotificationType.Comment,
                    message: $"{user.UserName} commented on your post.",
                    postId: post.Id
                );
            }

            await _db.SaveChangesAsync();

            await _userProgressService.AddPointsAsync(user, POINTS_FOR_NEW_COMMENT);
            var userId = user.Id;
            await _badgeService.CheckPostingBadgesAsync(userId);

            var updatedPost = await _db.Posts
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            return Partial("_CommentsList", updatedPost);
        }

        public async Task<IActionResult> OnPostDeletePostAsync(Guid entityId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var post = await _db.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p =>
                    p.Id == entityId &&
                    (p.UserId == user.Id || isAdmin));

            if (post == null)
                return NotFound();

            // --- NEW: DELETE THE PHYSICAL IMAGE FILE ---
            if (!string.IsNullOrEmpty(post.ImagePath))
            {
                // Extracts the filename (e.g., "guid.jpg") from your stored path (e.g., "/uploads/guid.jpg")
                var fileName = Path.GetFileName(post.ImagePath);
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            // -------------------------------------------

            var commentIds = post.Comments.Select(c => c.Id).ToList();

            var relatedReports = await _db.Reports
                .Where(r => r.PostId == entityId || (r.CommentId != null && commentIds.Contains(r.CommentId.Value)))
                .ToListAsync();

            foreach (var r in relatedReports)
            {
                r.PostId = null;
                r.CommentId = null;
                r.Status = ReportStatus.Resolved;
            }

            _db.Comments.RemoveRange(post.Comments);
            _db.Likes.RemoveRange(post.Likes);

            var relatedNotifications = await _db.Notifications
                .Where(n => n.PostId == post.Id)
                .ToListAsync();

            _db.Notifications.RemoveRange(relatedNotifications);

            _db.Posts.Remove(post);

            _db.AdminLogs.Add(new AdminLog
            {
                Action = "DELETE_POST",
                PerformedByUserId = user.Id,
                Details = isAdmin
                    ? $"Admin deleted post {post.Id}. Reason: {reason}"
                    : $"{user.UserName} deleted their own post {post.Id}"
            });

            if (isAdmin && post.UserId != user.Id)
            {
                await _notificationService.CreateAsync(
                    receiverId: post.UserId,
                    senderId: user.Id,
                    type: NotificationType.PostDeleted,
                    message: $"Your post was removed by admin. Reason: {reason}",
                    postId: null
                );
            }

            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Post and image deleted successfully.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // ◄ UPDATED: Any broken flight leg now completely triggers entire reservation cancellation
        public async Task<IActionResult> OnPostDeleteFlightAsync(Guid entityId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var flight = await _db.Flights
                .Include(f => f.Airline)
                .Include(f => f.Bookings)
                .FirstOrDefaultAsync(f => f.Id == entityId && (f.Airline.UserId == user.Id || isAdmin));

            if (flight == null) return NotFound();

            // Clear reports related directly to this flight leg
            var relatedReports = await _db.Reports.Where(r => r.FlightId == entityId).ToListAsync();
            foreach (var r in relatedReports)
            {
                r.FlightId = null;
                r.Status = ReportStatus.Resolved;
            }

            // Find all reservations that contain this flight leg
            var affectedReservationIds = await _db.Bookings
                .Where(b => b.FlightId == entityId)
                .Select(b => b.ReservationId)
                .Distinct()
                .ToListAsync();

            if (affectedReservationIds.Any())
            {
                // Fetch the complete reservations along with ALL their bookings, tickets, and flights
                var completeReservations = await _db.Reservations
                    .Where(r => affectedReservationIds.Contains(r.Id))
                    .Include(r => r.Bookings)
                        .ThenInclude(b => b.Flight)
                            .ThenInclude(f => f.ArrivalAirport)
                    .Include(r => r.Bookings)
                        .ThenInclude(b => b.Tickets)
                            .ThenInclude(t => t.BaggageItems)
                    .ToListAsync();

                foreach (var reservation in completeReservations)
                {
                    // Look up ultimate destination city name from the final leg of the complete journey
                    var ultimateFlightLeg = reservation.Bookings.OrderBy(b => b.Flight.DepartureTime).LastOrDefault()?.Flight;
                    string destinationCity = ultimateFlightLeg?.ArrivalAirport?.City ?? "your destination";

                    // Notify user about full cancellation
                    await _notificationService.CreateAsync(
                        receiverId: reservation.UserId,
                        senderId: user.Id,
                        type: NotificationType.FlightCancelled,
                        message: $"Your reservation for {destinationCity} was canceled entirely because one of its flights ({flight.FlightNumber}) was removed. Reason: {reason}",
                        postId: null
                    );

                    // Drop all cascading entities linked to every single booking leg in this reservation
                    foreach (var booking in reservation.Bookings)
                    {
                        if (booking.Tickets.Any())
                        {
                            var tickets = booking.Tickets.ToList();
                            var ticketIds = tickets.Select(t => t.Id).ToList();

                            var baggage = tickets.SelectMany(t => t.BaggageItems).ToList();
                            if (baggage.Any()) _db.BaggageItems.RemoveRange(baggage);

                            var flightSeats = await _db.FlightSeats.Where(fs => fs.TicketId != null && ticketIds.Contains(fs.TicketId.Value)).ToListAsync();
                            foreach (var fs in flightSeats)
                            {
                                fs.TicketId = null;
                                fs.Ticket = null;
                            }
                            _db.Tickets.RemoveRange(tickets);
                        }
                    }

                    _db.Bookings.RemoveRange(reservation.Bookings);
                    _db.Reservations.Remove(reservation);
                }

                // Flush changes to ensure database state registers wiped reservation references properly
                await _db.SaveChangesAsync();
            }

            // Remove independent flight context structures safely
            var remainingFlightSeats = await _db.FlightSeats.Where(fs => fs.FlightId == entityId).ToListAsync();
            if (remainingFlightSeats.Any()) _db.FlightSeats.RemoveRange(remainingFlightSeats);

            _db.Flights.Remove(flight);

            _db.AdminLogs.Add(new AdminLog
            {
                Action = "DELETE_FLIGHT",
                PerformedByUserId = user.Id,
                Details = isAdmin
                    ? $"Admin deleted flight {flight.FlightNumber}. Reason: {reason}"
                    : $"{user.UserName} deleted flight {flight.FlightNumber}"
            });

            if (isAdmin && flight.Airline.UserId != user.Id)
            {
                await _notificationService.CreateAsync(
                    receiverId: flight.Airline.UserId,
                    senderId: user.Id,
                    type: NotificationType.FlightCancelled,
                    message: $"Your flight {flight.FlightNumber} was removed by admin. Reason: {reason}",
                    postId: null
                );
            }

            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Flight deleted. All linked reservations have been completely cancelled and purged.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // ◄ UPDATED: Deleting an aircraft will now pull down the entire reservation if even one leg uses it
        public async Task<IActionResult> OnPostDeleteAircraftAsync(Guid entityId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var aircraft = await _db.Aircrafts
                .Include(a => a.Airline)
                .Include(a => a.Flights)
                .FirstOrDefaultAsync(a => a.Id == entityId);

            if (aircraft == null) return NotFound();
            if (!isAdmin && aircraft.Airline.UserId != user.Id) return Unauthorized();

            var flightsAssociated = aircraft.Flights.ToList();
            var flightIdsBeingDeleted = flightsAssociated.Select(f => f.Id).ToList();

            // Collect any reservation that relies on any flight operated by this aircraft asset
            var affectedReservationIds = await _db.Bookings
                .Where(b => flightIdsBeingDeleted.Contains(b.FlightId))
                .Select(b => b.ReservationId)
                .Distinct()
                .ToListAsync();

            if (affectedReservationIds.Any())
            {
                // Load whole reservation entities to cancel every single linked booking leg completely
                var completeReservations = await _db.Reservations
                    .Where(r => affectedReservationIds.Contains(r.Id))
                    .Include(r => r.Bookings)
                        .ThenInclude(b => b.Flight)
                            .ThenInclude(f => f.ArrivalAirport)
                    .Include(r => r.Bookings)
                        .ThenInclude(b => b.Tickets)
                            .ThenInclude(t => t.BaggageItems)
                    .ToListAsync();

                foreach (var reservation in completeReservations)
                {
                    var ultimateFlightLeg = reservation.Bookings.OrderBy(b => b.Flight.DepartureTime).LastOrDefault()?.Flight;
                    string destinationCity = ultimateFlightLeg?.ArrivalAirport?.City ?? "your destination";

                    await _notificationService.CreateAsync(
                        receiverId: reservation.UserId,
                        senderId: user.Id,
                        type: NotificationType.FlightCancelled,
                        message: $"Your reservation for {destinationCity} was canceled entirely because the scheduled aircraft equipment changed. Reason: {reason}",
                        postId: null
                    );

                    // Complete recursive data loop cleanup across all legs within this reservation container
                    foreach (var booking in reservation.Bookings)
                    {
                        if (booking.Tickets.Any())
                        {
                            var tickets = booking.Tickets.ToList();
                            var ticketIds = tickets.Select(t => t.Id).ToList();

                            var baggage = tickets.SelectMany(t => t.BaggageItems).ToList();
                            if (baggage.Any()) _db.BaggageItems.RemoveRange(baggage);

                            var flightSeats = await _db.FlightSeats.Where(fs => fs.TicketId != null && ticketIds.Contains(fs.TicketId.Value)).ToListAsync();
                            foreach (var fs in flightSeats)
                            {
                                fs.TicketId = null;
                                fs.Ticket = null;
                            }
                            _db.Tickets.RemoveRange(tickets);
                        }
                    }

                    _db.Bookings.RemoveRange(reservation.Bookings);
                    _db.Reservations.Remove(reservation);
                }

                await _db.SaveChangesAsync();
            }

            // Begin decoupling operational flight metrics schedules maps
            foreach (var flight in flightsAssociated)
            {
                var flightId = flight.Id;

                var relatedReports = await _db.Reports.Where(r => r.FlightId == flightId).ToListAsync();
                foreach (var r in relatedReports)
                {
                    r.FlightId = null;
                    r.Status = ReportStatus.Resolved;
                }

                var remainingFlightSeats = await _db.FlightSeats.Where(fs => fs.FlightId == flightId).ToListAsync();
                if (remainingFlightSeats.Any()) _db.FlightSeats.RemoveRange(remainingFlightSeats);

                _db.Flights.Remove(flight);
            }

            _db.Aircrafts.Remove(aircraft);

            _db.AdminLogs.Add(new AdminLog
            {
                Action = "DELETE_AIRCRAFT",
                PerformedByUserId = user.Id,
                Details = isAdmin
                    ? $"Admin deleted aircraft {aircraft.Model}. Reason: {reason}"
                    : $"{user.UserName} deleted aircraft {aircraft.Model}"
            });

            if (isAdmin && aircraft.Airline.UserId != user.Id)
            {
                await _notificationService.CreateAsync(
                    receiverId: aircraft.Airline.UserId,
                    senderId: user.Id,
                    type: NotificationType.FlightCancelled,
                    message: $"Your aircraft {aircraft.Model} was removed by admin. Reason: {reason}",
                    postId: null
                );
            }

            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Aircraft deleted. All affected reservations have been completely cancelled to avoid broken segments.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public async Task<IActionResult> OnPostDeleteCommentAsync(Guid entityId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var comment = await _db.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == entityId);

            if (comment == null) return NotFound();

            if (comment.UserId != user.Id && comment.Post.UserId != user.Id && !isAdmin)
            {
                return Unauthorized();
            }

            var relatedReports = await _db.Reports
                .Where(r => r.CommentId == entityId)
                .ToListAsync();

            foreach (var r in relatedReports)
            {
                r.CommentId = null;
                r.Status = ReportStatus.Resolved;
            }

            await _db.SaveChangesAsync();

            _db.Comments.Remove(comment);

            _db.AdminLogs.Add(new AdminLog
            {
                Action = "DELETE_COMMENT",
                PerformedByUserId = user.Id,
                Details = isAdmin
                    ? $"Admin deleted comment {comment.Id}. Reason: {reason}"
                    : $"{user.UserName} deleted comment {comment.Id}"
            });

            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Comment deleted successfully.";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public async Task<IActionResult> OnPostReportAsync(Guid entityId, string type, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var report = new Report
            {
                ReporterId = user.Id,
                Reason = reason
            };

            switch (type)
            {
                case "Post":
                    report.Type = ReportType.Post;
                    report.PostId = entityId;
                    break;

                case "Comment":
                    report.Type = ReportType.Comment;
                    report.CommentId = entityId;
                    break;

                case "Flight":
                    report.Type = ReportType.Flight;
                    report.FlightId = entityId;
                    break;

                default:
                    return BadRequest();
            }

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            _db.AdminLogs.Add(new AdminLog
            {
                Action = "REPORT_CREATED",
                PerformedByUserId = user.Id,
                Details = $"{user.UserName} reported {type} {entityId}. Reason: {reason}"
            });
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Report submitted.";
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}