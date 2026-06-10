using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Localization;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private readonly IApiClient  _api;
    private readonly LocalizationManager _loc;

    [ObservableProperty] private string _apiBaseUrl  = string.Empty;

    [ObservableProperty] private string _selectedLanguage = "vi";

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

    public SettingsViewModel(AppSettings settings, IApiClient api, LocalizationManager loc)
    {
        _settings = settings;
        _api      = api;
        _loc      = loc;
    }

    public void Initialize()
    {
        ApiBaseUrl  = _settings.ApiBaseUrl;
        SelectedLanguage = _settings.Language;
        ConnectionStatus = string.Empty;
        TestPassed  = null;
        SaveStatus  = string.Empty;
        SavePassed  = null;
        ClearError();
        _ = LoadMachinesAsync();
    }

    [RelayCommand]
    private void SetLanguage(string lang)
    {
        SelectedLanguage = lang;
        _settings.Language = lang;
        _loc.SetLanguage(lang);
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
        ConnectionStatus = _loc["Settings_Testing"];
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            // GET on the login endpoint — any HTTP response means server is reachable
            var resp = await client.GetAsync(ApiBaseUrl.TrimEnd('/') + "/api/v1/auth/login");
            TestPassed = true;
            ConnectionStatus = string.Format(_loc["Settings_TestSuccess"], (int)resp.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            TestPassed = false;
            ConnectionStatus = string.Format(_loc["Settings_TestFailedConnect"], ex.Message);
        }
        catch (TaskCanceledException)
        {
            TestPassed = false;
            ConnectionStatus = _loc["Settings_TestTimeout"];
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
            SaveStatus = _loc["Settings_SaveSelectMachine"];
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
                MachineName = _settings.MachineName,
                Language    = _settings.Language
            }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            SavePassed = true;
            SaveStatus = urlChanged
                ? _loc["Settings_SaveSuccessUrlChanged"]
                : _loc["Settings_SaveSuccess"];
        }
        catch (Exception ex)
        {
            SavePassed = false;
            SaveStatus = string.Format(_loc["Settings_SaveError"], ex.Message);
        }
    }
}
