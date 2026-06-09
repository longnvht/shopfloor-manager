using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace ShopfloorManager.Desktop.Services;

public class SignalRService : ISignalRService
{
    private HubConnection? _connection;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event Action<NcrCreatedPayload>? NcrCreated;

    public async Task ConnectAsync(string token, string baseUrl)
    {
        if (_connection is not null)
            await DisconnectAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl.TrimEnd('/')}/hub/shopfloor", opts =>
            {
                opts.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .AddJsonProtocol(opts =>
            {
                opts.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                opts.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<NcrCreatedPayload>("ncr-created", payload =>
            NcrCreated?.Invoke(payload));

        try { await _connection.StartAsync(); }
        catch { /* Non-critical — app works without real-time */ }
    }

    public async Task DisconnectAsync()
    {
        if (_connection is null) return;
        try { await _connection.StopAsync(); } catch { }
        await _connection.DisposeAsync();
        _connection = null;
    }
}
