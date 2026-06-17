using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Modules.Search.Entities;

namespace Cortex.Modules.Search.Persistence;

public interface IChatRepository
{
    Task<List<ChatSession>> GetSessionsAsync(Guid userId, CancellationToken ct = default);
    Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<List<ChatMessage>> GetMessagesAsync(Guid sessionId, CancellationToken ct = default);
    
    Task AddSessionAsync(ChatSession session, CancellationToken ct = default);
    Task AddMessageAsync(ChatMessage message, CancellationToken ct = default);
    Task DeleteSessionAsync(ChatSession session, CancellationToken ct = default);
}
