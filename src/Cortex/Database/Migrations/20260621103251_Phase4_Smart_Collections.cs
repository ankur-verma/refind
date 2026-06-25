using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_Smart_Collections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RuleDefinition",
                table: "AutoCollections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CollectionMemberships",
                columns: table => new
                {
                    AutoCollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionMemberships", x => new { x.AutoCollectionId, x.ContentItemId });
                    table.ForeignKey(
                        name: "FK_CollectionMemberships_AutoCollections_AutoCollectionId",
                        column: x => x.AutoCollectionId,
                        principalTable: "AutoCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectionMemberships_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionMemberships_ContentItemId",
                table: "CollectionMemberships",
                column: "ContentItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionMemberships");

            migrationBuilder.DropColumn(
                name: "RuleDefinition",
                table: "AutoCollections");
        }
    }
}
