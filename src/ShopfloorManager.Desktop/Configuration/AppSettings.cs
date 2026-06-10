namespace ShopfloorManager.Desktop.Configuration;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5066";
    public string MachineCode { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string Language { get; set; } = "vi";
}
