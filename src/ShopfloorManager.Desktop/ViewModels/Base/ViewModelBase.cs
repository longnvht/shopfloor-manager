using CommunityToolkit.Mvvm.ComponentModel;

namespace ShopfloorManager.Desktop.ViewModels.Base;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    protected void ClearError() => ErrorMessage = string.Empty;
}
