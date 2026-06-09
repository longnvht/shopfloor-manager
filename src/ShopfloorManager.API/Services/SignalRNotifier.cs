using Microsoft.AspNetCore.SignalR;
using ShopfloorManager.API.Hubs;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Application.Production;

namespace ShopfloorManager.API.Services;

public class SignalRNotifier(IHubContext<ShopfloorHub> hub) : IRealtimeNotifier
{
    public async Task NotifyNcrCreatedAsync(NcrDto ncr, CancellationToken ct)
    {
        try
        {
            await hub.Clients
                .Groups(["role:QC Inspector", "role:Administrator", "role:Manager"])
                .SendAsync("ncr-created", ncr, ct);
        }
        catch { /* non-critical */ }
    }

    public async Task NotifyMeasureSubmittedAsync(MeasureValueDto measure, CancellationToken ct)
    {
        try
        {
            await hub.Clients
                .Groups(["role:QC Inspector", "role:Engineer", "role:Administrator"])
                .SendAsync("measure-submitted", measure, ct);
        }
        catch { /* non-critical */ }
    }
}
