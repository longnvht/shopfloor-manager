using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private readonly IApiClient  _api;

    [ObservableProperty] private string _apiBaseUrl  = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private bool _isTesting;

    [ObservableProperty] private string _connectionStatus = string.Empty;
    [ObservableProperty] private bool?  _testPassed;          // null=untested, true=ok, false=fail

    [ObservableProperty] private string _saveStatus  = string.Empty;
    [ObservableProperty] private bool?  _savePassed;           // null=not yet, true=ok, false=error

    // Machine selection
    public ObservableCollection<MachineDto> Machines { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMachineName))]
    private MachineDto? _selectedMachine;

    public string SelectedMachineName => SelectedMachine?.Name ?? string.Empty;

    public string AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public Action? OnBack { get; set; }

    public SettingsViewModel(AppSettings settings, IApiClient api)
    {
        _settings = settings;
        _api      = api;
    }

    public void Initialize()
    {
        ApiBaseUrl  = _settings.ApiBaseUrl;
        ConnectionStatus = string.Empty;
        TestPassed  = null;
        SaveStatus  = string.Empty;
        SavePassed  = null;
        ClearError();
        _ = LoadMachinesAsync();
    }

    private async Task LoadMachinesAsync()
    {
        try
        {
            var result = await _api.GetAsync<List<MachineDto>>("/api/v1/machines?activeOnly=true");
            if (result?.Success == true && result.Data is not null)
            {
                Machines.Clear();
                foreach (var m in result.Data)
                    Machines.Add(m);

                // Pre-select current machine
                SelectedMachine = Machines.FirstOrDefault(m => m.Code == _settings.MachineCode);
            }
        }
        catch { /* API offline — user can still edit URL */ }
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
        if (SelectedMachine is null)
        {
            SavePassed = false;
            SaveStatus = "Vui lòng chọn mã máy.";
            return;
        }

        var urlChanged = _settings.ApiBaseUrl != ApiBaseUrl.TrimEnd('/');

        _settings.ApiBaseUrl  = ApiBaseUrl.TrimEnd('/');
        _settings.MachineCode = SelectedMachine.Code;
        _settings.MachineName = SelectedMachine.Name;

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
