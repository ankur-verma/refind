using System;
using System.Collections.Generic;

namespace Cortex.Modules.Search.DTOs;

public class ChatSessionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<RAGSourceDto>? Sources { get; set; }
}

public class CreateChatSessionRequest
{
    public string InitialMessage { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
}
