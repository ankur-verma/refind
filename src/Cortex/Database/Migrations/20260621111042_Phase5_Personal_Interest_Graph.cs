using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_Personal_Interest_Graph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInterestProfiles");

            migrationBuilder.CreateTable(
                name: "UserBehaviorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBehaviorEvents_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserBehaviorEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInterests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Trend = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastCalculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInterests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterestHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserInterestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousScore = table.Column<int>(type: "integer", nullable: false),
                    NewScore = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterestHistories_UserInterests_UserInterestId",
                        column: x => x.UserInterestId,
                        principalTable: "UserInterests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterestHistories_UserInterestId",
                table: "InterestHistories",
                column: "UserInterestId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorEvents_ContentItemId",
                table: "UserBehaviorEvents",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorEvents_UserId",
                table: "UserBehaviorEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInterests_UserId",
                table: "UserInterests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterestHistories");

            migrationBuilder.DropTable(
                name: "UserBehaviorEvents");

            migrationBuilder.DropTable(
                name: "UserInterests");

            migrationBuilder.CreateTable(
                name: "UserInterestProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastCalculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Trend = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterestProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInterestProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserInterestProfiles_UserId",
                table: "UserInterestProfiles",
                column: "UserId");
        }
    }
}
