using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Modules.Content.Persistence.EntityConfigurations;

public class ContentPayloadConfiguration : IEntityTypeConfiguration<ContentPayload>
{
    public void Configure(EntityTypeBuilder<ContentPayload> builder)
    {
        builder.ToTable("ContentPayloads");
        builder.HasKey(x => x.ContentItemId);
        builder.Property(x => x.GeminiEmbedding).HasColumnType("vector(1536)");
        builder.Property(x => x.OllamaEmbedding).HasColumnType("vector(768)");

        // HNSW vector indices for rapid similarity search
        builder.HasIndex(x => x.GeminiEmbedding)
            .HasDatabaseName("idx_contentpayloads_gemini_embedding")
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.HasIndex(x => x.OllamaEmbedding)
            .HasDatabaseName("idx_contentpayloads_ollama_embedding")
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
