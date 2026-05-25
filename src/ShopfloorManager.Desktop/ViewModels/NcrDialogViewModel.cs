using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class NcrDialogViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private List<NcrReasonDto> _allReasons = [];

    public NcrTriggerArgs Args { get; }

    public string BalloonSummary => Args.MeasuredValue.HasValue
        ? $"Balloon {Args.BalloonNumber}  |  Đo: {Args.MeasuredValue:F4}  |  Cho phép: {Args.MinValue:F4} ~ {Args.MaxValue:F4}"
        : $"Balloon {Args.BalloonNumber}  |  Kết quả: FAIL";

    public ObservableCollection<DepartmentLookupDto> Departments { get; } = [];
    public ObservableCollection<NcrReasonDto> FilteredReasons { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateNcrCommand))]
    [NotifyPropertyChangedFor(nameof(ShowReasons))]
    private DepartmentLookupDto? _selectedDepartment;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateNcrCommand))]
    [NotifyPropertyChangedFor(nameof(IsOtherSelected))]
    [NotifyPropertyChangedFor(nameof(DescriptionLabel))]
    private NcrReasonDto? _selectedReason;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateNcrCommand))]
    private string _description = "";

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = "";

    public bool ShowReasons    => SelectedDepartment is not null;
    public bool IsOtherSelected => SelectedReason?.Id == OtherId;
    public string DescriptionLabel => IsOtherSelected ? "MÔ TẢ *" : "GHI CHÚ (tùy chọn)";

    private const int OtherId = -1;
    public bool NcrCreated { get; private set; }
    public Action? OnClose { get; set; }

    public NcrDialogViewModel(IApiClient api, NcrTriggerArgs args)
    {
        _api = api;
        Args = args;
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var deptTask   = _api.GetAsync<List<DepartmentLookupDto>>("/api/v1/departments");
            var reasonTask = _api.GetAsync<List<NcrReasonDto>>("/api/v1/ncr-reasons");
            await Task.WhenAll(deptTask, reasonTask);

            var depts   = deptTask.Result?.Data   ?? [];
            var reasons = reasonTask.Result?.Data ?? [];

            foreach (var d in depts)   Departments.Add(d);
            _allReasons = reasons;
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    partial void OnSelectedDepartmentChanged(DepartmentLookupDto? value)
    {
        SelectedReason = null;
        FilteredReasons.Clear();
        if (value is null) return;
        foreach (var r in _allReasons.Where(r => r.DepartmentId == (int?)value.Id))
            FilteredReasons.Add(r);
        FilteredReasons.Add(new NcrReasonDto(OtherId, "Khác", null, value.Id));
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateNcrAsync()
    {
        if (SelectedReason is null) return;
        IsBusy = true; ErrorMessage = "";
        try
        {
            var desc = string.IsNullOrWhiteSpace(Description)
                ? $"Balloon {Args.BalloonNumber} FAIL"
                : Description;

            var req = new
            {
                JobId        = Args.JobId,
                ProductId    = (int?)Args.ProductId,
                PartOpId     = (int?)Args.PartOpId,
                ReasonId     = SelectedReason.Id == OtherId ? (int?)null : SelectedReason.Id,
                DepartmentId = (int?)SelectedDepartment!.Id,
                MachineCode  = Args.MachineCode,
                Description  = desc
            };

            var resp = await _api.PostAsync<object, object>("/api/v1/ncrs", req);
            if (resp?.Success == true)
            {
                NcrCreated = true;
                OnClose?.Invoke();
            }
            else
                ErrorMessage = resp?.Error ?? "Không thể tạo NCR.";
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private bool CanCreate() => SelectedReason is not null && !IsBusy &&
                                (!IsOtherSelected || !string.IsNullOrWhiteSpace(Description));

    [RelayCommand]
    private void Skip() => OnClose?.Invoke();
}
