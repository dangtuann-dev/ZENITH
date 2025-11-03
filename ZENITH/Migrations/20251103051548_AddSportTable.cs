using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZENITH.Migrations
{
    /// <inheritdoc />
    public partial class AddSportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SportId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sports",
                columns: table => new
                {
                    SportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SportName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sports", x => x.SportId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SportId",
                table: "Products",
                column: "SportId");

            migrationBuilder.CreateIndex(
                name: "IX_sports_DisplayOrder",
                table: "sports",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_sports_SportName",
                table: "sports",
                column: "SportName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_sports_SportId",
                table: "Products",
                column: "SportId",
                principalTable: "sports",
                principalColumn: "SportId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_sports_SportId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "sports");

            migrationBuilder.DropIndex(
                name: "IX_Products_SportId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SportId",
                table: "Products");
        }
    }
}
