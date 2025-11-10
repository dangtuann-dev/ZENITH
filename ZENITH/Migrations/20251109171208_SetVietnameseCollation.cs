using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZENITH.Migrations
{
    /// <inheritdoc />
    public partial class SetVietnameseCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase(
                collation: "Vietnamese_CI_AS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase(
                oldCollation: "Vietnamese_CI_AS");
        }
    }
}
