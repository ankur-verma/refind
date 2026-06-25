using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class VideoIntelligencePlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "VideoSegments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<VideoSegmentMetadata>(
                name: "Metadata",
                table: "VideoSegments",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "SegmentType",
                table: "VideoSegments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "General");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "VideoSegments");

            migrationBuilder.DropColumn(
                name: "SegmentType",
                table: "VideoSegments");

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "VideoSegments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
