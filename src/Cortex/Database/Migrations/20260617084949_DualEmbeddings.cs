using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Cortex.Database.Migrations
{
    /// <inheritdoc />
    public partial class DualEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SemanticEmbedding",
                table: "ContentPayloads",
                newName: "GeminiEmbedding");

            migrationBuilder.RenameIndex(
                name: "idx_contentpayloads_embedding",
                table: "ContentPayloads",
                newName: "idx_contentpayloads_gemini_embedding");

            migrationBuilder.AddColumn<Vector>(
                name: "OllamaEmbedding",
                table: "ContentPayloads",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_contentpayloads_ollama_embedding",
                table: "ContentPayloads",
                column: "OllamaEmbedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_contentpayloads_ollama_embedding",
                table: "ContentPayloads");

            migrationBuilder.DropColumn(
                name: "OllamaEmbedding",
                table: "ContentPayloads");

            migrationBuilder.RenameColumn(
                name: "GeminiEmbedding",
                table: "ContentPayloads",
                newName: "SemanticEmbedding");

            migrationBuilder.RenameIndex(
                name: "idx_contentpayloads_gemini_embedding",
                table: "ContentPayloads",
                newName: "idx_contentpayloads_embedding");
        }
    }
}
