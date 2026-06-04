using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ShopfloorManager.API.Hubs;

/// <summary>
/// Hub cho CNC Live — web client subscribe nhận real-time machine status.
/// Client join group "machine_{machineCode}" hoặc "all_machines".
/// </summary>
[Authorize]
public class MachineStatusHub : Hub
{
    public async Task JoinMachineGroup(string machineCode)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"machine_{machineCode}");

    public async Task LeaveAllMachineGroups()
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_machines");

    public override async Task OnConnectedAsync()
    {
        // Auto-join "all_machines" group on connect
        await Groups.AddToGroupAsync(Context.ConnectionId, "all_machines");
        await base.OnConnectedAsync();
    }
}
