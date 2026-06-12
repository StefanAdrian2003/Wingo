using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Pages;
using Xunit;

namespace Proiect_Licenta.Tests
{
    public class BookingPassengerTests
    {
        private ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task OnPostAsync_NoSeats_ReturnsPageWithModelError()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var db = CreateInMemoryContext(dbName);

            var pageModel = new BookingPassengerModel(db);

            // Act: pass empty selection "[]"
            var result = await pageModel.OnPostAsync(
                Guid.NewGuid(),    // id
                null,              // returnId
                null,              // leg2Id
                null,              // retLeg2Id
                "[]",              // selectedSeats (empty)
                null,              // leg2Seats
                null,              // returnSeats
                null               // retLeg2Seats
            );

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(pageModel.ModelState.IsValid);
            Assert.Contains(pageModel.ModelState, kvp => kvp.Value.Errors.Count > 0);
        }

        [Fact]
        public async Task OnPostAsync_Leg2SeatCountMismatch_ReturnsPageWithModelError()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var db = CreateInMemoryContext(dbName);

            var pageModel = new BookingPassengerModel(db);

            // outbound has 2 seats, leg2 has 1 -> mismatch
            var selectedSeatsJson = System.Text.Json.JsonSerializer.Serialize(new[] { Guid.NewGuid(), Guid.NewGuid() });
            var leg2SeatsJson = System.Text.Json.JsonSerializer.Serialize(new[] { Guid.NewGuid() });

            // Act
            var result = await pageModel.OnPostAsync(
                Guid.NewGuid(),   // id
                null,             // returnId
                Guid.NewGuid(),   // leg2Id (present)
                null,             // retLeg2Id
                selectedSeatsJson,
                leg2SeatsJson,
                null,
                null
            );

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(pageModel.ModelState.IsValid);
            Assert.Contains(pageModel.ModelState, kvp => kvp.Value.Errors.Count > 0);
        }

        [Fact]
        public async Task OnPostAsync_ReturnSeatCountMismatch_ReturnsPageWithModelError()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var db = CreateInMemoryContext(dbName);

            var pageModel = new BookingPassengerModel(db);

            // outbound has 2 seats, return has 1 -> mismatch
            var selectedSeatsJson = System.Text.Json.JsonSerializer.Serialize(new[] { Guid.NewGuid(), Guid.NewGuid() });
            var returnSeatsJson = System.Text.Json.JsonSerializer.Serialize(new[] { Guid.NewGuid() });

            // Act
            var result = await pageModel.OnPostAsync(
                Guid.NewGuid(),    // id
                Guid.NewGuid(),    // returnId (present)
                null,              // leg2Id
                null,              // retLeg2Id
                selectedSeatsJson,
                null,
                returnSeatsJson,
                null
            );

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(pageModel.ModelState.IsValid);
            Assert.Contains(pageModel.ModelState, kvp => kvp.Value.Errors.Count > 0);
        }
    }
}