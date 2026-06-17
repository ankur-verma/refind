using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Modules.Search.DTOs;
using Cortex.Modules.Search.Entities;
using Cortex.Modules.Search.Persistence;
using Cortex.Shared;

namespace Cortex.Modules.Search.UseCases;

public class GlobalChatUseCase
{
    private readonly IChatRepository _chatRepository;
    private readonly RAGQueryUseCase _ragQueryUseCase;

    public GlobalChatUseCase(IChatRepository chatRepository, RAGQueryUseCase ragQueryUseCase)
    {
        _chatRepository = chatRepository;
        _ragQueryUseCase = ragQueryUseCase;
    }

    public async Task<Result<List<ChatSessionDto>>> GetSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _chatRepository.GetSessionsAsync(userId, ct);
        return Result.Success(sessions.Select(x => new ChatSessionDto
        {
            Id = x.Id,
            Title = x.Title,
            CreatedAt = x.CreatedAt
        }).ToList());
    }

    public async Task<Result<List<ChatMessageDto>>> GetMessagesAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _chatRepository.GetSessionByIdAsync(sessionId, ct);
        if (session == null || session.UserId != userId)
            return Result.Failure<List<ChatMessageDto>>("Chat session not found or unauthorized.");

        var messages = await _chatRepository.GetMessagesAsync(sessionId, ct);
        
        var dtos = messages.Select(x => new ChatMessageDto
        {
            Id = x.Id,
            Role = x.Role,
            Content = x.Content,
            CreatedAt = x.CreatedAt,
            Sources = string.IsNullOrEmpty(x.SourcesJson) ? null : JsonSerializer.Deserialize<List<RAGSourceDto>>(x.SourcesJson)
        }).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<ChatSessionDto>> CreateSessionAsync(Guid userId, CreateChatSessionRequest request, CancellationToken ct = default)
    {
        var session = new ChatSession
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.InitialMessage) ? "New Chat" : 
                    (request.InitialMessage.Length > 40 ? request.InitialMessage.Substring(0, 40) + "..." : request.InitialMessage)
        };

        await _chatRepository.AddSessionAsync(session, ct);

        return Result.Success(new ChatSessionDto
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt
        });
    }

    public async Task<Result> DeleteSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _chatRepository.GetSessionByIdAsync(sessionId, ct);
        if (session == null || session.UserId != userId)
            return Result.Failure("Chat session not found or unauthorized.");

        await _chatRepository.DeleteSessionAsync(session, ct);
        return Result.Success();
    }

    public async Task<Result<ChatMessageDto>> SendMessageAsync(Guid userId, Guid sessionId, SendMessageRequest request, CancellationToken ct = default)
    {
        var session = await _chatRepository.GetSessionByIdAsync(sessionId, ct);
        if (session == null || session.UserId != userId)
            return Result.Failure<ChatMessageDto>("Chat session not found or unauthorized.");

        // Add User Message
        var userMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = "User",
            Content = request.Message
        };
        await _chatRepository.AddMessageAsync(userMessage, ct);

        // Fetch past messages to inject context (optional, but RAGQueryUseCase currently only takes 'Query')
        // For now, we just pass the user message to the RAGQueryUseCase as the single query.
        // A true conversational RAG might take history into account, but we'll stick to the existing RAGUseCase first.
        
        var ragRequest = new RAGQueryRequest
        {
            Query = request.Message,
            Mode = "Hybrid"
        };
        
        var ragResult = await _ragQueryUseCase.ExecuteAsync(userId, ragRequest, ct);

        string assistantAnswer = "I'm sorry, I couldn't process your request.";
        string? sourcesJson = null;

        if (ragResult.IsSuccess && ragResult.Value != null)
        {
            assistantAnswer = ragResult.Value.Answer;
            if (ragResult.Value.Sources.Any())
            {
                sourcesJson = JsonSerializer.Serialize(ragResult.Value.Sources);
            }
        }

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = "Assistant",
            Content = assistantAnswer,
            SourcesJson = sourcesJson
        };
        await _chatRepository.AddMessageAsync(assistantMessage, ct);

        return Result.Success(new ChatMessageDto
        {
            Id = assistantMessage.Id,
            Role = assistantMessage.Role,
            Content = assistantMessage.Content,
            CreatedAt = assistantMessage.CreatedAt,
            Sources = string.IsNullOrEmpty(sourcesJson) ? null : JsonSerializer.Deserialize<List<RAGSourceDto>>(sourcesJson)
        });
    }
}
