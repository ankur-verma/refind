using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Search.Persistence;

public class ChatRepository : IChatRepository
{
    private readonly CortexDbContext _dbContext;

    public ChatRepository(CortexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ChatSession>> GetSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.ChatSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ChatSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _dbContext.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct);
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _dbContext.ChatMessages
            .Where(x => x.ChatSessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddSessionAsync(ChatSession session, CancellationToken ct = default)
    {
        _dbContext.ChatSessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteSessionAsync(ChatSession session, CancellationToken ct = default)
    {
        _dbContext.ChatSessions.Remove(session);
        await _dbContext.SaveChangesAsync(ct);
    }
}
