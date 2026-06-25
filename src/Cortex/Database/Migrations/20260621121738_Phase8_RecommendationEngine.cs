using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_RecommendationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecommendations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRecommendations_RecommendationType",
                table: "UserRecommendations",
                column: "RecommendationType");

            migrationBuilder.CreateIndex(
                name: "IX_UserRecommendations_UserId",
                table: "UserRecommendations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRecommendations");
        }
    }
}
