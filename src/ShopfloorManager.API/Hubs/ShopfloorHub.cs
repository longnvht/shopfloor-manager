using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ShopfloorManager.API.Hubs;

/// <summary>
/// Real-time hub — dùng cho: NCR notifications, dashboard updates, machine status.
/// Client kết nối tại: /hub/shopfloor
/// </summary>
[Authorize]
public class ShopfloorHub : Hub
{
    /// <summary>Client join vào group theo role hoặc machine.</summary>
    public async Task JoinGroup(string groupName) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    public async Task LeaveGroup(string groupName) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
        await base.OnConnectedAsync();
    }
}
