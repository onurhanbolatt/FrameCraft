using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrameCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSchemaNameFromTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_SchemaName",
                schema: "dbo",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SchemaName",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SchemaName",
                schema: "dbo",
                table: "Tenants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 12, 15, 3, 56, 303, DateTimeKind.Utc).AddTicks(9189));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 12, 15, 3, 56, 303, DateTimeKind.Utc).AddTicks(9670));

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_SchemaName",
                schema: "dbo",
                table: "Tenants",
                column: "SchemaName",
                unique: true);
        }
    }
}
