using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;

    [ObservableProperty] private string _apiBaseUrl  = string.Empty;
    [ObservableProperty] private string _machineCode = string.Empty;
    [ObservableProperty] private string _machineName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private bool _isTesting;

    [ObservableProperty] private string _connectionStatus = string.Empty;
    [ObservableProperty] private bool?  _testPassed;          // null=untested, true=ok, false=fail

    [ObservableProperty] private string _saveStatus  = string.Empty;
    [ObservableProperty] private bool?  _savePassed;           // null=not yet, true=ok, false=error

    public string AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public Action? OnBack { get; set; }

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
    }

    public void Initialize()
    {
        ApiBaseUrl  = _settings.ApiBaseUrl;
        MachineCode = _settings.MachineCode;
        MachineName = _settings.MachineName;
        ConnectionStatus = string.Empty;
        TestPassed  = null;
        SaveStatus  = string.Empty;
        SavePassed  = null;
        ClearError();
    }

    [RelayCommand]
    private void Back() => OnBack?.Invoke();

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestPassed = null;
        ConnectionStatus = "Đang kiểm tra...";
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            // GET on the login endpoint — any HTTP response means server is reachable
            var resp = await client.GetAsync(ApiBaseUrl.TrimEnd('/') + "/api/v1/auth/login");
            TestPassed = true;
            ConnectionStatus = $"Kết nối thành công (HTTP {(int)resp.StatusCode})";
        }
        catch (HttpRequestException ex)
        {
            TestPassed = false;
            ConnectionStatus = $"Không kết nối được: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            TestPassed = false;
            ConnectionStatus = "Hết thời gian chờ (timeout 5s)";
        }
        finally { IsTesting = false; }
    }

    private bool CanTest() => !IsTesting;

    [RelayCommand]
    private void Save()
    {
        var urlChanged = _settings.ApiBaseUrl != ApiBaseUrl.TrimEnd('/');

        _settings.ApiBaseUrl  = ApiBaseUrl.TrimEnd('/');
        _settings.MachineCode = MachineCode.Trim();
        _settings.MachineName = MachineName.Trim();

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "local.json");
            var json = JsonSerializer.Serialize(new
            {
                ApiBaseUrl  = _settings.ApiBaseUrl,
                MachineCode = _settings.MachineCode,
                MachineName = _settings.MachineName
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            SavePassed = true;
            SaveStatus = urlChanged
                ? "Đã lưu. Khởi động lại ứng dụng để áp dụng URL API mới."
                : "Đã lưu cấu hình thành công.";
        }
        catch (Exception ex)
        {
            SavePassed = false;
            SaveStatus = $"Lỗi ghi file: {ex.Message}";
        }
    }
}
