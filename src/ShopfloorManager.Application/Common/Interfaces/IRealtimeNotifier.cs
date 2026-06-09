using ShopfloorManager.Application.Production;

namespace ShopfloorManager.Application.Common.Interfaces;

/// <summary>
/// Broadcast real-time events đến các Desktop/Web clients qua SignalR.
/// Interface ở Application layer — implementation ở API layer (SignalRNotifier).
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyNcrCreatedAsync(NcrDto ncr, CancellationToken ct = default);
    Task NotifyMeasureSubmittedAsync(MeasureValueDto measure, CancellationToken ct = default);
}
