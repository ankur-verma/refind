using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_AI_Content_Understanding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    SubCategory = table.Column<string>(type: "text", nullable: false),
                    Intent = table.Column<string>(type: "text", nullable: false),
                    Sentiment = table.Column<string>(type: "text", nullable: false),
                    Topics = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentInsights_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SemanticEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentInsightBrands",
                columns: table => new
                {
                    ContentInsightId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInsightBrands", x => new { x.ContentInsightId, x.BrandId });
                    table.ForeignKey(
                        name: "FK_ContentInsightBrands_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentInsightBrands_ContentInsights_ContentInsightId",
                        column: x => x.ContentInsightId,
                        principalTable: "ContentInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentInsightLocations",
                columns: table => new
                {
                    ContentInsightId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInsightLocations", x => new { x.ContentInsightId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_ContentInsightLocations_ContentInsights_ContentInsightId",
                        column: x => x.ContentInsightId,
                        principalTable: "ContentInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentInsightLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentInsightProducts",
                columns: table => new
                {
                    ContentInsightId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInsightProducts", x => new { x.ContentInsightId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_ContentInsightProducts_ContentInsights_ContentInsightId",
                        column: x => x.ContentInsightId,
                        principalTable: "ContentInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentInsightProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentInsightEntities",
                columns: table => new
                {
                    ContentInsightId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemanticEntityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInsightEntities", x => new { x.ContentInsightId, x.SemanticEntityId });
                    table.ForeignKey(
                        name: "FK_ContentInsightEntities_ContentInsights_ContentInsightId",
                        column: x => x.ContentInsightId,
                        principalTable: "ContentInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentInsightEntities_SemanticEntities_SemanticEntityId",
                        column: x => x.SemanticEntityId,
                        principalTable: "SemanticEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentInsightBrands_BrandId",
                table: "ContentInsightBrands",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInsightEntities_SemanticEntityId",
                table: "ContentInsightEntities",
                column: "SemanticEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInsightLocations_LocationId",
                table: "ContentInsightLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInsightProducts_ProductId",
                table: "ContentInsightProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInsights_ContentItemId",
                table: "ContentInsights",
                column: "ContentItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentInsightBrands");

            migrationBuilder.DropTable(
                name: "ContentInsightEntities");

            migrationBuilder.DropTable(
                name: "ContentInsightLocations");

            migrationBuilder.DropTable(
                name: "ContentInsightProducts");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "SemanticEntities");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "ContentInsights");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
