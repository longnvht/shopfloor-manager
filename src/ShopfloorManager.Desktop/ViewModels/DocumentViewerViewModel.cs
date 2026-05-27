using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Models;
using ShopfloorManager.Desktop.Services;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class DocumentViewerViewModel : Base.ViewModelBase
{
    private readonly IApiClient _api;

    public JobSummaryDto? Job { get; private set; }
    public PartOpDto?     Op  { get; private set; }

    public string TitleContext => Job is null ? "" :
        $"{Job.JobNumber}  ·  OP {Op?.OpNumber}  ·  {Job.PartNumber} Rev {Job.RevCode}";

    public ObservableCollection<TechDocDto> Documents { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDoc))]
    [NotifyPropertyChangedFor(nameof(IsGcodeSelected))]
    [NotifyPropertyChangedFor(nameof(IsNonGcodeSelected))]
    [NotifyPropertyChangedFor(nameof(SelectedDocLabel))]
    private TechDocDto? _selectedDocument;

    [ObservableProperty] private string  _gcodeText    = "";
    [ObservableProperty] private bool    _isLoadingContent;
    [ObservableProperty] private string  _contentError = "";
    [ObservableProperty] private bool    _noDocuments;

    public bool HasSelectedDoc     => SelectedDocument is not null;
    public bool IsGcodeSelected    => SelectedDocument?.IsGcodeType == true;
    public bool IsNonGcodeSelected => HasSelectedDoc && !IsGcodeSelected;
    public string SelectedDocLabel => SelectedDocument?.DisplayName ?? "";

    public Action? OnBack { get; set; }

    public DocumentViewerViewModel(IApiClient api)
    {
        _api = api;
    }

    public async Task InitializeAsync(JobSummaryDto job, PartOpDto op)
    {
        Job = job; Op = op;
        OnPropertyChanged(nameof(TitleContext));
        await LoadDocumentsAsync();
    }

    private async Task LoadDocumentsAsync()
    {
        IsBusy = true; ClearError();
        Documents.Clear();
        GcodeText = ""; ContentError = ""; NoDocuments = false;
        try
        {
            var resp = await _api.GetAsync<List<TechDocDto>>(
                $"/api/v1/tech-documents?partOpId={Op!.Id}&status=Approved");

            if (resp?.Data is { Count: > 0 })
            {
                foreach (var d in resp.Data) Documents.Add(d);
                // Auto-select first G-code, or first doc
                SelectedDocument = Documents.FirstOrDefault(d => d.IsGcodeType)
                                ?? Documents.First();
            }
            else
            {
                NoDocuments = true;
            }
        }
        catch (Exception ex) { ErrorMessage = $"Không thể tải danh sách tài liệu: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    partial void OnSelectedDocumentChanged(TechDocDto? value)
    {
        GcodeText = ""; ContentError = "";
        if (value?.IsGcodeType == true)
            _ = LoadGcodeAsync(value);
    }

    private async Task LoadGcodeAsync(TechDocDto doc)
    {
        IsLoadingContent = true; ContentError = "";
        try
        {
            // Lấy presigned download URL từ API
            var urlResp = await _api.GetAsync<string>($"/api/v1/tech-documents/{doc.Id}/download-url");
            if (urlResp?.Data is null) { ContentError = "Không lấy được đường dẫn tải file."; return; }

            // Dùng HttpClient mới không có Bearer token — MinIO presigned URL đã có auth sẵn,
            // Bearer token trong shared HttpClient sẽ gây lỗi 400/403 từ MinIO.
            using var client = new System.Net.Http.HttpClient();
            var content = await client.GetStringAsync(urlResp.Data);
            GcodeText = content;
        }
        catch (Exception ex) { ContentError = $"Không thể tải file: {ex.Message}"; }
        finally { IsLoadingContent = false; }
    }

    [RelayCommand]
    private void GoBack() => OnBack?.Invoke();
}
