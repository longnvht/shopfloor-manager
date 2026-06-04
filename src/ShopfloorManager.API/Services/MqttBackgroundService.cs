using System.Buffers;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using ShopfloorManager.Domain.Entities;
using ShopfloorManager.API.Hubs;
using ShopfloorManager.Infrastructure.Data;
using ShopfloorManager.Infrastructure.Mqtt;

namespace ShopfloorManager.API.Services;

public class MqttOptions
{
    public string Broker { get; set; } = "localhost";
    public int    Port   { get; set; } = 1883;
    public string Topic  { get; set; } = "factory/cnc/#";
}

public class MqttBackgroundService(
    IServiceProvider      services,
    IMemoryCache          cache,
    IHubContext<MachineStatusHub> hub,
    IOptions<MqttOptions> opts,
    ILogger<MqttBackgroundService> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var o = opts.Value;
        using var client = new MqttClientFactory().CreateMqttClient();

        client.ApplicationMessageReceivedAsync += msg => HandleMessageAsync(msg, ct);
        client.DisconnectedAsync += async _ =>
        {
            logger.LogWarning("MQTT disconnected — retrying in 5s");
            await Task.Delay(5_000, ct);
            if (!ct.IsCancellationRequested) await TryConnectAsync(client, o, ct);
        };

        await TryConnectAsync(client, o, ct);
        await Task.Delay(Timeout.Infinite, ct);
    }

    private static async Task TryConnectAsync(IMqttClient client, MqttOptions o, CancellationToken ct)
    {
        try
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(o.Broker, o.Port)
                .WithClientId($"shopfloor-api-{Guid.NewGuid():N}")
                .WithCleanSession(false)
                .Build();

            await client.ConnectAsync(options, ct);
            await client.SubscribeAsync(o.Topic, cancellationToken: ct);
        }
        catch { /* retry via DisconnectedAsync */ }
    }

    private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e, CancellationToken ct)
    {
        try
        {
            // v5: Payload is ReadOnlySequence<byte>
            var seq = e.ApplicationMessage.Payload;
            if (seq.IsEmpty) return;
            var payloadBytes = seq.IsSingleSegment
                ? seq.First.Span
                : (ReadOnlySpan<byte>)seq.ToArray();
            var payload = JsonSerializer.Deserialize<CncPayload>(payloadBytes, _json);
            if (payload is null) return;

            // Staleness check
            if ((DateTimeOffset.UtcNow - payload.Timestamp).TotalMinutes > 10) return;

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ShopfloorDbContext>();

            var machine = await db.Machines
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Code == payload.MachineCode, ct);
            if (machine is null)
            {
                logger.LogDebug("Unknown machine code: {Code}", payload.MachineCode);
                return;
            }

            // State-change detection — only write event when state changes
            var cacheKey = $"machine_status_{machine.Code}";
            var prev = cache.Get<CncPayload>(cacheKey);

            if (HasStateChanged(prev, payload))
            {
                var evt = new MachineEvent
                {
                    MachineId    = machine.Id,
                    TmMode       = payload.TmMode,
                    AtMode       = payload.AtMode,
                    RunMode      = payload.RunMode,
                    Alarm        = payload.Alarm,
                    AlarmMessage = payload.AlarmMessage,
                    CreatedAt    = payload.Timestamp,
                };
                db.MachineEvents.Add(evt);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Machine {Code} state change: {Mode}", machine.Code, payload.RunMode);
            }

            // Cache latest status (5 min TTL — machines send data continuously)
            cache.Set(cacheKey, payload, TimeSpan.FromMinutes(5));

            // Broadcast to web clients subscribed to this machine's group
            var dto = new MachineStatusDto(
                machine.Id, machine.Code, machine.Name,
                payload.RunMode, payload.TmMode, payload.AlarmMessage,
                payload.SpindleSpeed, payload.SpindleLoad, payload.Feedrate,
                payload.XPosition, payload.YPosition, payload.ZPosition,
                payload.Program, payload.PartCount, payload.Timestamp);

            await hub.Clients.Group($"machine_{machine.Code}")
                .SendAsync("MachineStatusUpdated", dto, ct);

            // Also broadcast to "all_machines" group for dashboard overview
            await hub.Clients.Group("all_machines")
                .SendAsync("MachineStatusUpdated", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing MQTT message");
        }
    }

    private static bool HasStateChanged(CncPayload? prev, CncPayload curr)
    {
        if (prev is null) return true;
        return prev.RunMode      != curr.RunMode
            || prev.TmMode       != curr.TmMode
            || prev.Alarm        != curr.Alarm
            || prev.AlarmMessage != curr.AlarmMessage;
    }
}
