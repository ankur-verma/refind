using System;
using Cortex.Shared;
using Cortex.Modules.Auth.Entities;
using Pgvector;

namespace Cortex.Modules.Search.Entities;

public class UserMLProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public Vector? UserEmbedding { get; set; } // Aggregate vector (1536 dim if Gemini/OpenAI, or 384 for sentence-transformers)
    public int? ClusterId { get; set; }
    public DateTime? LastComputedAt { get; set; }

    public User User { get; set; } = null!;
}
