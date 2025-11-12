﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZENITH.Migrations
{
    /// <inheritdoc />
    public partial class ReviewUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                WITH cte AS (
                    SELECT ReviewId,
                           ROW_NUMBER() OVER (PARTITION BY UserId, ProductId ORDER BY CreatedAt DESC, ReviewId DESC) AS rn
                    FROM Reviews
                )
                DELETE FROM Reviews WHERE ReviewId IN (SELECT ReviewId FROM cte WHERE rn > 1);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_ProductId",
                table: "Reviews",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_UserId_ProductId",
                table: "Reviews");
        }
    }
}
