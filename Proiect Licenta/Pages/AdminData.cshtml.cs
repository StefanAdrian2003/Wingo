using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Pages
{
    public class AdminDataModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public AdminDataModel(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // USERS
        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }

        // FLIGHTS
        public int TotalFlights { get; set; }
        public int TotalAircrafts { get; set; }
        public int TotalBookings { get; set; }

        // SOCIAL
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }

        // OTHER
        public int TotalBadges { get; set; }
        public int TotalNotifications { get; set; }

        public List<AdminLog> Logs { get; set; } = new();
        public List<Report> Reports { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalUsers = await _db.Users.CountAsync(u => !u.IsCompany);
            TotalCompanies = await _db.Users.CountAsync(u => u.IsCompany);

            TotalFlights = await _db.Flights.CountAsync();
            TotalAircrafts = await _db.Aircrafts.CountAsync();
            TotalBookings = await _db.Bookings.CountAsync();

            TotalPosts = await _db.Posts.CountAsync();
            TotalComments = await _db.Comments.CountAsync();
            TotalLikes = await _db.Likes.CountAsync();

            TotalBadges = await _db.Badges.CountAsync();
            TotalNotifications = await _db.Notifications.CountAsync();

            Logs = await _db.AdminLogs
                .Include(l => l.PerformedByUser)
                .OrderByDescending(l => l.DateOfCreation)
                .Take(50)
                .ToListAsync();

            Reports = await _db.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReviewedByAdmin)
                .Include(r => r.Post).ThenInclude(p => p.User)
                .Include(r => r.Post).ThenInclude(p => p.Likes)
                .Include(r => r.Post).ThenInclude(p => p.Comments)
                .Include(r => r.Comment).ThenInclude(c => c.User)
                .Include(r => r.Comment).ThenInclude(c => c.Post).ThenInclude(p => p.User)
                .Include(r => r.Flight).ThenInclude(f => f.Airline).ThenInclude(a => a.User)
                .Include(r => r.Flight).ThenInclude(f => f.DepartureAirport)
                .Include(r => r.Flight).ThenInclude(f => f.ArrivalAirport)
                .Include(r => r.Flight).ThenInclude(f => f.Aircraft)
                .OrderByDescending(r => r.DateOfCreation)
                .ToListAsync();
        }

        // MARK AS VIEWED (AJAX)
        public async Task<IActionResult> OnPostMarkAsViewedAsync(Guid reportId)
        {
            var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return NotFound();

            if (report.Status == ReportStatus.Pending)
            {
                report.Status = ReportStatus.Reviewed;
                await _db.SaveChangesAsync();
            }

            return new JsonResult(new { success = true });
        }

        // RESOLVE REPORT
        public async Task<IActionResult> OnPostResolveReportAsync(Guid reportId, string actionType)
        {
            var report = await _db.Reports
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return NotFound();

            if (report.Status == ReportStatus.Resolved || report.Status == ReportStatus.Rejected)
            {
                return BadRequest("Report already finalized.");
            }

            var adminId = _userManager.GetUserId(User);
            var reporterId = report.ReporterId;

            // ============================================================================
            // APPROVE (DELETE CONTENT)
            // ============================================================================
            if (actionType == "approve")
            {
                // ------------------------------------------------------------------------
                // CASE 1: REPORTED CONTENT IS A POST
                // ------------------------------------------------------------------------
                if (report.PostId != null)
                {
                    var postId = report.PostId.Value;

                    var post = await _db.Posts
                        .Include(p => p.Comments)
                        .Include(p => p.Likes)
                        .FirstOrDefaultAsync(p => p.Id == postId);

                    if (post != null)
                    {
                        var commentIds = post.Comments.Select(c => c.Id).ToList();

                        // Dezlegăm toate rapoartele legate de această postare sau de comentariile ei
                        var relatedReports = await _db.Reports
                            .Where(r => r.PostId == postId || (r.CommentId != null && commentIds.Contains(r.CommentId.Value)))
                            .ToListAsync();

                        foreach (var r in relatedReports)
                        {
                            r.PostId = null;
                            r.CommentId = null;
                            r.Status = ReportStatus.Resolved;
                            r.ReviewedByAdminId = adminId;
                        }

                        // Salvăm modificările stării rapoartelor pentru a rupe constrângerile Foreign Key
                        await _db.SaveChangesAsync();

                        // Trimitem notificare creatorului postării
                        if (!string.IsNullOrEmpty(post.UserId))
                        {
                            _db.Notifications.Add(new Notification
                            {
                                UserId = post.UserId,
                                SenderId = adminId,
                                Type = NotificationType.PostDeleted,
                                Message = "Your post was deleted due to violating our community guidelines.",
                                DateOfCreation = DateTime.UtcNow,
                                IsRead = false
                            });
                        }

                        // Ștergem datele dependente și postarea
                        _db.Comments.RemoveRange(post.Comments);
                        _db.Likes.RemoveRange(post.Likes);

                        var relatedNotifications = await _db.Notifications.Where(n => n.PostId == postId).ToListAsync();
                        _db.Notifications.RemoveRange(relatedNotifications);

                        _db.Posts.Remove(post);

                        // Înregistrăm acțiunea în loguri
                        _db.AdminLogs.Add(new AdminLog
                        {
                            Action = "ADMIN_APPROVE_REPORT_POST",
                            PerformedByUserId = adminId,
                            Details = $"Admin approved report {reportId} and deleted post {postId}."
                        });
                    }
                }

                // ------------------------------------------------------------------------
                // CASE 2: REPORTED CONTENT IS A COMMENT
                // ------------------------------------------------------------------------
                else if (report.CommentId != null)
                {
                    var commentId = report.CommentId.Value;

                    var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);

                    if (comment != null)
                    {
                        // Dezlegăm toate rapoartele deschise pe acest comentariu specific
                        var relatedCommentReports = await _db.Reports
                            .Where(r => r.CommentId == commentId)
                            .ToListAsync();

                        foreach (var r in relatedCommentReports)
                        {
                            r.CommentId = null;
                            r.Status = ReportStatus.Resolved;
                            r.ReviewedByAdminId = adminId;
                        }

                        // Aplicăm deconectarea cheilor străine
                        await _db.SaveChangesAsync();

                        // Notificăm autorul comentariului
                        if (!string.IsNullOrEmpty(comment.UserId))
                        {
                            _db.Notifications.Add(new Notification
                            {
                                UserId = comment.UserId,
                                SenderId = adminId,
                                Type = NotificationType.CommentDeleted,
                                Message = "Your comment was deleted due to violating our community guidelines.",
                                DateOfCreation = DateTime.UtcNow,
                                IsRead = false
                            });
                        }

                        // Eliminăm comentariul fizic
                        _db.Comments.Remove(comment);

                        _db.AdminLogs.Add(new AdminLog
                        {
                            Action = "ADMIN_APPROVE_REPORT_COMMENT",
                            PerformedByUserId = adminId,
                            Details = $"Admin approved report {reportId} and deleted comment {commentId}."
                        });
                    }
                }

                // ------------------------------------------------------------------------
                // CASE 3: REPORTED CONTENT IS A FLIGHT
                // ------------------------------------------------------------------------
                else if (report.FlightId != null)
                {
                    var flightId = report.FlightId.Value;

                    var flight = await _db.Flights
                        .Include(f => f.Airline)
                        .Include(f => f.Bookings)
                        .FirstOrDefaultAsync(f => f.Id == flightId);

                    if (flight != null)
                    {
                        // Dezlegăm rapoartele atașate de acest zbor
                        var relatedFlightReports = await _db.Reports
                            .Where(r => r.FlightId == flightId)
                            .ToListAsync();

                        foreach (var r in relatedFlightReports)
                        {
                            r.FlightId = null;
                            r.Status = ReportStatus.Resolved;
                            r.ReviewedByAdminId = adminId;
                        }

                        // Notificăm pasagerii zborului anulat
                        foreach (var booking in flight.Bookings)
                        {
                            _db.Notifications.Add(new Notification
                            {
                                UserId = booking.UserId,
                                SenderId = adminId,
                                Type = NotificationType.FlightCancelled,
                                Message = $"The flight {flight.FlightNumber} you booked has been removed by system administration.",
                                DateOfCreation = DateTime.UtcNow,
                                IsRead = false
                            });
                        }

                        // Notificăm Compania Aeriană
                        if (flight.Airline != null && !string.IsNullOrEmpty(flight.Airline.UserId))
                        {
                            _db.Notifications.Add(new Notification
                            {
                                UserId = flight.Airline.UserId,
                                SenderId = adminId,
                                Type = NotificationType.FlightCancelled,
                                Message = $"Your flight {flight.FlightNumber} was removed due to violating our community guidelines.",
                                DateOfCreation = DateTime.UtcNow,
                                IsRead = false
                            });
                        }

                        // Ștergem rezervările care blocau constrângerea Restrict, apoi zborul
                        _db.Bookings.RemoveRange(flight.Bookings);
                        _db.Flights.Remove(flight);

                        _db.AdminLogs.Add(new AdminLog
                        {
                            Action = "ADMIN_APPROVE_REPORT_FLIGHT",
                            PerformedByUserId = adminId,
                            Details = $"Admin approved report {reportId} and deleted flight {flightId} ({flight.FlightNumber})."
                        });
                    }
                }

                // Trimitem notificarea globală de succes către cel ce a raportat (Reporter)
                _db.Notifications.Add(new Notification
                {
                    UserId = reporterId,
                    SenderId = adminId,
                    Type = NotificationType.ReportApproved,
                    Message = "Thank you! The content you reported has been reviewed and removed.",
                    DateOfCreation = DateTime.UtcNow,
                    IsRead = false
                });

                // Setăm raportul curent ca fiind soluționat (Approved)
                report.Status = ReportStatus.Resolved;
                report.ReviewedByAdminId = adminId;
            }

            // ============================================================================
            // REJECT (KEEP CONTENT)
            // ============================================================================
            else if (actionType == "reject")
            {
                report.Status = ReportStatus.Rejected;
                report.ReviewedByAdminId = adminId;

                // Notificăm reporterul că sesizarea lui a fost respinsă de admin
                _db.Notifications.Add(new Notification
                {
                    UserId = reporterId,
                    SenderId = adminId,
                    Type = NotificationType.ReportRejected,
                    Message = "The reported content was reviewed by administration and respects our community guidelines.",
                    PostId = report.PostId,
                    DateOfCreation = DateTime.UtcNow,
                    IsRead = false
                });

                _db.AdminLogs.Add(new AdminLog
                {
                    Action = "ADMIN_REJECT_REPORT",
                    PerformedByUserId = adminId,
                    Details = $"Admin rejected report {reportId}."
                });
            }
            else
            {
                return BadRequest("Invalid action type.");
            }

            // Salvăm modificările finale în baza de date
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] = "Report processed successfully.";
            return RedirectToPage();
        }
    }
}