namespace ShopfloorManager.Desktop.Services;

public record NcrCreatedPayload(
    string NcrNumber, string JobNumber,
    string? SerialNumber, string Description, string RaisedBy);

public interface ISignalRService
{
    bool IsConnected { get; }
    event Action<NcrCreatedPayload>? NcrCreated;
    Task ConnectAsync(string token, string baseUrl);
    Task DisconnectAsync();
}
