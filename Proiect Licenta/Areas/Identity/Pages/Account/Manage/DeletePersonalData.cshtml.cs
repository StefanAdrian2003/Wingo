// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DeletePersonalDataModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // 🛑 CRITICAL STRATEGIC GUARDRAIL: Admin accounts cannot be hard deleted
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                ModelState.AddModelError(string.Empty, "Administrative accounts cannot be deleted to preserve system audit logs and operational data integrity. Please contact system management.");
                return Page();
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            var isCompany = await _userManager.IsInRoleAsync(user, "Company");
            var userId = await _userManager.GetUserIdAsync(user);

            // Fetch company details early to evaluate the profile/logo path string if applicable
            var airline = isCompany ? await _context.Airlines.FirstOrDefaultAsync(a => a.UserId == user.Id) : null;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 🎛️ ─── STEP 1: AUDIT LOGS & ADMIN LINKS (NULLIFY) ───
                var adminLogs = await _context.AdminLogs
                    .Where(al => al.PerformedByUserId == user.Id)
                    .ToListAsync();
                foreach (var log in adminLogs)
                {
                    log.PerformedByUserId = null;
                }

                var reviewedReports = await _context.Reports
                    .Where(r => r.ReviewedByAdminId == user.Id)
                    .ToListAsync();
                foreach (var report in reviewedReports)
                {
                    report.ReviewedByAdminId = null;
                }
                await _context.SaveChangesAsync();


                // 📋 ─── STEP 2: COLLECT ALL TARGET ENTITY IDs AHEAD OF TIME ───
                var postIds = await _context.Posts.Where(p => p.UserId == user.Id).Select(p => p.Id).ToListAsync();
                var commentIds = await _context.Comments.Where(c => c.UserId == user.Id || postIds.Contains(c.PostId)).Select(c => c.Id).ToListAsync();

                var flightIds = airline != null ? await _context.Flights.Where(f => f.AirlineId == airline.Id).Select(f => f.Id).ToListAsync() : new List<Guid>();


                // 🚨 ─── STEP 3: PURGE ALL MODERATION REPORTS SYSTEM-WIDE ───
                var allRelatedReports = await _context.Reports
                    .Where(r => r.ReporterId == user.Id ||
                                (r.PostId != null && postIds.Contains(r.PostId.Value)) ||
                                (r.CommentId != null && commentIds.Contains(r.CommentId.Value)) ||
                                (r.FlightId != null && flightIds.Contains(r.FlightId.Value)))
                    .ToListAsync();
                _context.Reports.RemoveRange(allRelatedReports);
                await _context.SaveChangesAsync();


                // 📊 ─── STEP 4: CLEAN UP SOCIAL ENGAGEMENT (LIKES, COMMENTS, POSTS) ───
                var engagementLikes = await _context.Likes
                    .Where(l => l.UserId == user.Id || postIds.Contains(l.PostId))
                    .ToListAsync();
                _context.Likes.RemoveRange(engagementLikes);

                var communityComments = await _context.Comments.Where(c => commentIds.Contains(c.Id)).ToListAsync();
                _context.Comments.RemoveRange(communityComments);

                var communityPosts = await _context.Posts.Where(p => postIds.Contains(p.Id)).ToListAsync();

                // Wipe physical images associated with user's posts
                foreach (var post in communityPosts)
                {
                    if (!string.IsNullOrEmpty(post.ImagePath))
                    {
                        var postImageFileName = Path.GetFileName(post.ImagePath);
                        var postImagePath = Path.Combine(_environment.WebRootPath, "uploads", postImageFileName);

                        if (System.IO.File.Exists(postImagePath))
                        {
                            System.IO.File.Delete(postImagePath);
                        }
                    }
                }

                _context.Posts.RemoveRange(communityPosts);

                var userNotifications = await _context.Notifications.Where(n => n.UserId == user.Id || n.SenderId == user.Id).ToListAsync();
                _context.Notifications.RemoveRange(userNotifications);

                var userBadges = await _context.UserBadges.Where(ub => ub.UserId == user.Id).ToListAsync();
                _context.UserBadges.RemoveRange(userBadges);

                await _context.SaveChangesAsync();


                // ✈️ ─── STEP 5: BUSINESS DOMAIN HARD CLEANUP ───
                if (isCompany && airline != null)
                {
                    if (flightIds.Any())
                    {
                        // Identify all distinct reservations affected by this airline's flights
                        var affectedReservationIds = await _context.Bookings
                            .Where(b => flightIds.Contains(b.FlightId))
                            .Select(b => b.ReservationId)
                            .Distinct()
                            .ToListAsync();

                        if (affectedReservationIds.Any())
                        {
                            // Load whole reservation entities to execute clean recursive deletes across ALL legs
                            var completeReservations = await _context.Reservations
                                .Where(r => affectedReservationIds.Contains(r.Id))
                                .Include(r => r.Bookings)
                                    .ThenInclude(b => b.Flight)
                                .Include(r => r.Bookings)
                                    .ThenInclude(b => b.Tickets)
                                        .ThenInclude(t => t.BaggageItems)
                                .ToListAsync();

                            foreach (var reservation in completeReservations)
                            {
                                // Notify about full journey cancellation due to account deletion
                                var targetFlightLeg = reservation.Bookings.FirstOrDefault(b => flightIds.Contains(b.FlightId))?.Flight;
                                string flightDesignator = targetFlightLeg != null ? targetFlightLeg.FlightNumber : "scheduled flight segment";

                                _context.Notifications.Add(new Notification
                                {
                                    Type = NotificationType.FlightCancelled,
                                    IsRead = false,
                                    Message = $"Your entire reservation was canceled because the operating partner for flight {flightDesignator} deleted their account.",
                                    UserId = reservation.UserId,
                                    SenderId = null
                                });

                                // Drop all cascading entities linked to every single leg of this reservation container
                                foreach (var booking in reservation.Bookings)
                                {
                                    if (booking.Tickets.Any())
                                    {
                                        var tickets = booking.Tickets.ToList();
                                        var ticketIds = tickets.Select(t => t.Id).ToList();

                                        var baggage = tickets.SelectMany(t => t.BaggageItems).ToList();
                                        if (baggage.Any()) _context.BaggageItems.RemoveRange(baggage);

                                        var flightSeats = await _context.FlightSeats
                                            .Where(fs => fs.TicketId != null && ticketIds.Contains(fs.TicketId.Value))
                                            .ToListAsync();

                                        foreach (var fs in flightSeats)
                                        {
                                            fs.TicketId = null;
                                        }

                                        _context.Tickets.RemoveRange(tickets);
                                    }
                                }

                                _context.Bookings.RemoveRange(reservation.Bookings);
                                _context.Reservations.Remove(reservation);
                            }

                            await _context.SaveChangesAsync();
                        }

                        // Wipe out remaining physical flight structures safely
                        var remainingFlightSeats = await _context.FlightSeats.Where(fs => flightIds.Contains(fs.FlightId)).ToListAsync();
                        if (remainingFlightSeats.Any()) _context.FlightSeats.RemoveRange(remainingFlightSeats);

                        var realFlights = await _context.Flights.Where(f => flightIds.Contains(f.Id)).ToListAsync();
                        _context.Flights.RemoveRange(realFlights);
                    }

                    // Teardown corporate fleet blueprints & aircraft inventory configurations
                    var fleetIds = await _context.Aircrafts.Where(a => a.AirlineId == airline.Id).Select(a => a.Id).ToListAsync();
                    if (fleetIds.Any())
                    {
                        var layoutSections = await _context.SeatSections.Where(ss => fleetIds.Contains(ss.AircraftId)).ToListAsync();
                        var sectionIds = layoutSections.Select(ss => ss.Id).ToList();

                        if (sectionIds.Any())
                        {
                            var structuralSeats = await _context.Seats.Where(s => sectionIds.Contains(s.SeatSectionId)).ToListAsync();
                            var structuralFlightSeats = await _context.FlightSeats.Where(fs => structuralSeats.Select(s => s.Id).Contains(fs.SeatId)).ToListAsync();

                            _context.FlightSeats.RemoveRange(structuralFlightSeats);
                            _context.Seats.RemoveRange(structuralSeats);
                            _context.SeatSections.RemoveRange(layoutSections);
                        }

                        var targetFleet = await _context.Aircrafts.Where(a => fleetIds.Contains(a.Id)).ToListAsync();
                        _context.Aircrafts.RemoveRange(targetFleet);
                    }

                    // --- UPDATED: WIPE PHYSICAL COMPANY LOGO VIA THE AIRLINE TABLE ---
                    if (!string.IsNullOrEmpty(airline.LogoUrl))
                    {
                        var logoFileName = Path.GetFileName(airline.LogoUrl);

                        // Assumes logo is stored in wwwroot/Logos. Change folder name here if it's different.
                        var logoFilePath = Path.Combine(_environment.WebRootPath, "Logos", logoFileName);

                        if (System.IO.File.Exists(logoFilePath))
                        {
                            System.IO.File.Delete(logoFilePath);
                        }
                    }

                    _context.Airlines.Remove(airline);
                }
                else
                {
                    // 🧳 PASSENGER CLIENT DATA WIPE
                    var clientReservations = await _context.Reservations.Where(r => r.UserId == user.Id).Select(r => r.Id).ToListAsync();
                    if (clientReservations.Any())
                    {
                        var clientBookingIds = await _context.Bookings.Where(b => clientReservations.Contains(b.ReservationId)).Select(b => b.Id).ToListAsync();
                        if (clientBookingIds.Any())
                        {
                            var clientTicketIds = await _context.Tickets.Where(t => clientBookingIds.Contains(t.BookingId)).Select(t => t.Id).ToListAsync();
                            if (clientTicketIds.Any())
                            {
                                var structuralReleaseSeats = await _context.FlightSeats.Where(fs => fs.TicketId != null && clientTicketIds.Contains(fs.TicketId.Value)).ToListAsync();
                                foreach (var fs in structuralReleaseSeats)
                                {
                                    fs.TicketId = null;
                                }

                                var clientBaggage = await _context.BaggageItems.Where(b => clientTicketIds.Contains(b.TicketId)).ToListAsync();
                                _context.BaggageItems.RemoveRange(clientBaggage);

                                var physicalTickets = await _context.Tickets.Where(t => clientTicketIds.Contains(t.Id)).ToListAsync();
                                _context.Tickets.RemoveRange(physicalTickets);
                            }
                            var physicalBookings = await _context.Bookings.Where(b => clientBookingIds.Contains(b.Id)).ToListAsync();
                            _context.Bookings.RemoveRange(physicalBookings);
                        }
                        var physicalReservations = await _context.Reservations.Where(r => clientReservations.Contains(r.Id)).ToListAsync();
                        _context.Reservations.RemoveRange(physicalReservations);
                    }

                    var customerDiscountTokens = await _context.Vouchers.Where(v => v.UserId == user.Id).ToListAsync();
                    _context.Vouchers.RemoveRange(customerDiscountTokens);
                }

                await _context.SaveChangesAsync();

                // 🗑️ ─── STEP 6: DELETE IDENTITY ACCOUNT ───
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Unexpected error occurred deleting user.");
                }

                await transaction.CommitAsync();

                await _signInManager.SignOutAsync();
                _logger.LogInformation("User with ID '{UserId}' deleted completely.", userId);

                return Redirect("~/");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction aborted during data cleanup processing for user: {Email}", user.Email);
                ModelState.AddModelError(string.Empty, $"Cascading error: {ex.InnerException?.Message ?? ex.Message}");
                return Page();
            }
        }
    }
}