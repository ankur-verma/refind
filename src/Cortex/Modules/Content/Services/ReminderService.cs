using System;
using System.Threading.Tasks;
using Cortex.Modules.Content.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Cortex.Modules.Content.Services;

public interface IReminderService
{
    Task PushMemoryNotificationAsync(Guid userId, string title, string message, string? actionUrl);
}

public class ReminderService : IReminderService
{
    private readonly IHubContext<ContentHub> _hubContext;

    public ReminderService(IHubContext<ContentHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushMemoryNotificationAsync(Guid userId, string title, string message, string? actionUrl)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveProactiveMemory", new 
        {
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Timestamp = DateTime.UtcNow
        });
    }
}
