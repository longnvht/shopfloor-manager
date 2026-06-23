using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;
    private readonly IApiClient _api;
    private readonly WorkContext _work;
    private readonly AppSettings _settings;
    private readonly ISignalRService _signalR;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    public LoginViewModel(IAuthService auth, INavigationService nav,
        IApiClient api, WorkContext work, AppSettings settings, ISignalRService signalR)
    {
        _auth     = auth;
        _nav      = nav;
        _api      = api;
        _work     = work;
        _settings = settings;
        _signalR  = signalR;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ClearError();
        IsBusy = true;
        try
        {
            var result = await _auth.LoginAsync(Username, Password);
            if (!result.Success)
            {
                ErrorMessage = result.Error ?? "Đăng nhập thất bại";
                return;
            }

            // Kết nối SignalR sau khi đăng nhập thành công (non-blocking)
            _ = _signalR.ConnectAsync(_auth.Token!, _settings.ApiBaseUrl);

            await DetermineAppMode();
            _nav.NavigateTo<MainViewModel>();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DetermineAppMode()
    {
        try
        {
            var resp = await _api.GetAsync<ActiveSessionDto>(
                $"/api/v1/machines/{_settings.MachineCode}/active-session");

            var active = resp?.Data;
            if (active is null)
            {
                _work.Mode = CanBeginSession ? AppMode.Operation : AppMode.View;
                return;
            }

            _work.IncomingSession = active;

            if (active.ClaimedBy == _auth.UserId)
            {
                // Resume own session
                _work.Mode = AppMode.Operation;
                RestoreWorkContext(active);
            }
            else
            {
                // Another user's session — Leader/Manager/Admin can force-finish (still Operation mode),
                // Operators enter View mode
                var isElevated = _auth.Role is "Leader" or "Manager" or "Administrator";
                _work.Mode = isElevated ? AppMode.Operation : AppMode.View;
            }
        }
        catch
        {
            // If the check fails, fall back based on role only (no session info available)
            _work.Mode = CanBeginSession ? AppMode.Operation : AppMode.View;
        }
    }

    /// <summary>Chỉ Operator/Leader/Administrator được tạo session gia công mới — QC/Engineer/Manager chỉ inspect/view.</summary>
    private bool CanBeginSession => _auth.Role is "Operator" or "Leader" or "Administrator";

    private void RestoreWorkContext(ActiveSessionDto active)
    {
        // Reconstruct minimal DTOs from the active session data
        var job = new JobSummaryDto(
            active.JobId, active.JobNumber, active.PartNumber,
            RevCode: "—", RoutingRevCode: null, RunQty: null,
            CompletedCount: 0,
            ShipBy: null, IsComplete: false,
            CreatedAt: active.ClaimedAt);

        var op = new PartOpDto(
            active.PartOpId, RoutingRevId: null, JobId: null,
            ForJobOnly: false, active.OpNumber, OpNumberSort: null,
            OpTypeId: null, OpTypeName: null, OpTypeCode: null, Description: null,
            Note: null, SetupTime: null, ProdTime: null,
            IsVisible: true, IsComplete: false);

        var product = new ProductWithSessionDto(
            active.ProductId, active.SerialNumber, SortOrder: 0,
            active.SessionId, active.Status, active.MachineCode,
            active.ClaimedAt, active.StartedAt, CompletedAt: null);

        var session = new ProductionSessionDto(
            active.SessionId, active.ProductId, active.SerialNumber,
            active.PartOpId, active.MachineCode, active.Status,
            active.ClaimedAt, active.StartedAt, CompletedAt: null,
            active.ClaimedBy, CancelledBy: null, Note: null);

        _work.SetJob(job);
        _work.SetOp(op);
        _work.SetProduct(product, session);
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
}
