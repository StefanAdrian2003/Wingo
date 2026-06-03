using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proiect_Licenta.Migrations
{
    /// <inheritdoc />
    public partial class AplicareVouchere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7740));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7754));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7757));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000020"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7759));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7762));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7764));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000250"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7766));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000500"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7768));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000001000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7771));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7773));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7775));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000020"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7778));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7780));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7782));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7784));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000010"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7785));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7789));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7791));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000500"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7792));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000001000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7794));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000005000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7796));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7797));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7799));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000015"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7801));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7804));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7806));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000250"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7807));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000500"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7809));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000001000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 3, 22, 26, 45, 746, DateTimeKind.Utc).AddTicks(7811));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Vouchers");

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4072));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4086));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4089));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000020"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4092));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4095));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4098));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000250"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4101));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000500"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000001000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4108));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4110));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4112));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000020"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4114));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4115));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4117));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4119));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000010"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4121));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4125));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4127));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000500"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4128));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000001000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4130));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000005000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4132));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4134));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4135));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000015"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4137));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000050"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4140));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000100"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4142));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000250"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4144));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000500"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4145));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000001000"),
                column: "DateOfCreation",
                value: new DateTime(2026, 6, 2, 19, 49, 14, 769, DateTimeKind.Utc).AddTicks(4147));
        }
    }
}
