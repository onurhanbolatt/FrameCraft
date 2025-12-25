using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrameCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSystemTenantToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemTenant",
                schema: "dbo",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 21, 14, 1, 11, 328, DateTimeKind.Utc).AddTicks(3301));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 21, 14, 1, 11, 328, DateTimeKind.Utc).AddTicks(3541));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemTenant",
                schema: "dbo",
                table: "Tenants");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 13, 0, 52, 33, 855, DateTimeKind.Utc).AddTicks(2272));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 13, 0, 52, 33, 855, DateTimeKind.Utc).AddTicks(2496));
        }
    }
}
