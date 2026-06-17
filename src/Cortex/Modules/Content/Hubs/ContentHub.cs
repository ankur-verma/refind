using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cortex.Modules.Content.Hubs;

[Authorize]
public class ContentHub : Hub
{
    // The Hub is mapped to /hubs/content
    // We can use Groups to isolate users to their own channels if needed,
    // but sending to Clients.User(userId) handles it automatically if NameIdentifier is mapped.
    
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
