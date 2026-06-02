using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Behaviors;
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
    [NotifyPropertyChangedFor(nameof(IsPdfViewerVisible))]
    [NotifyPropertyChangedFor(nameof(SelectedDocLabel))]
    [NotifyPropertyChangedFor(nameof(ShowTextViewer))]
    [NotifyPropertyChangedFor(nameof(ShowToolpathViewer))]
    private TechDocDto? _selectedDocument;

    [ObservableProperty] private string  _gcodeText    = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPdfViewerVisible))]
    private bool _isLoadingContent;

    [ObservableProperty] private string  _contentError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPdfViewerVisible))]
    private string? _pdfUrl;
    [ObservableProperty] private bool    _noDocuments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTextViewer))]
    [NotifyPropertyChangedFor(nameof(ShowToolpathViewer))]
    private bool _showToolpath;

    [ObservableProperty] private List<ToolpathMove> _toolpathMoves = [];

    public bool HasSelectedDoc      => SelectedDocument is not null;
    public bool IsGcodeSelected     => SelectedDocument?.IsGcodeType == true;
    public bool IsNonGcodeSelected  => HasSelectedDoc && !IsGcodeSelected;
    public bool IsPdfViewerVisible  => IsNonGcodeSelected && PdfUrl is not null && !IsLoadingContent;
    public string SelectedDocLabel  => SelectedDocument?.DisplayName ?? "";
    public bool ShowTextViewer      => IsGcodeSelected && !ShowToolpath;
    public bool ShowToolpathViewer  => IsGcodeSelected && ShowToolpath;

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
        ShowToolpath = false;
        GcodeText = ""; ContentError = ""; PdfUrl = null;
        ToolpathMoves = [];
        if (value?.IsGcodeType == true)
            _ = LoadGcodeAsync(value);
        else if (value is not null)
            _ = LoadPdfAsync(value);
    }

    private async Task LoadGcodeAsync(TechDocDto doc)
    {
        IsLoadingContent = true; ContentError = "";
        try
        {
            var urlResp = await _api.GetAsync<string>($"/api/v1/tech-documents/{doc.Id}/download-url");
            if (urlResp?.Data is null) { ContentError = "Không lấy được đường dẫn tải file."; return; }

            // MinIO presigned URL đã có auth — phải dùng HttpClient riêng không có Bearer header.
            using var client = new System.Net.Http.HttpClient();
            var content = await client.GetStringAsync(urlResp.Data);
            GcodeText     = content;
            ToolpathMoves = GcodeToolpathParser.Parse(content);
        }
        catch (Exception ex) { ContentError = $"Không thể tải file: {ex.Message}"; }
        finally { IsLoadingContent = false; }
    }

    private async Task LoadPdfAsync(TechDocDto doc)
    {
        IsLoadingContent = true; ContentError = "";
        try
        {
            var urlResp = await _api.GetAsync<string>($"/api/v1/tech-documents/{doc.Id}/download-url");
            if (urlResp?.Data is null) { ContentError = "Không lấy được đường dẫn tải file."; return; }
            PdfUrl = urlResp.Data;
        }
        catch (Exception ex) { ContentError = $"Không thể tải file: {ex.Message}"; }
        finally { IsLoadingContent = false; }
    }

    [RelayCommand] private void SetTextView()     => ShowToolpath = false;
    [RelayCommand] private void SetToolpathView() => ShowToolpath = true;

    [RelayCommand]
    private void GoBack() => OnBack?.Invoke();
}
