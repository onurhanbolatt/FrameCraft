using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrameCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Folder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                });

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
                name: "IX_UploadedFiles_Category",
                table: "UploadedFiles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_CreatedAt",
                table: "UploadedFiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_EntityId_EntityType",
                table: "UploadedFiles",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_FileKey",
                table: "UploadedFiles",
                column: "FileKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_Folder",
                table: "UploadedFiles",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_TenantId",
                table: "UploadedFiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UploadedBy",
                table: "UploadedFiles",
                column: "UploadedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 23, 36, 12, 669, DateTimeKind.Utc).AddTicks(3344));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 6, 23, 36, 12, 669, DateTimeKind.Utc).AddTicks(3631));
        }
    }
}
