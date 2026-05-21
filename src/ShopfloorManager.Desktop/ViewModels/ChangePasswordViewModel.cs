using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.ViewModels;

public partial class ChangePasswordViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly INavigationService _nav;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangeCommand))]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangeCommand))]
    private string _confirmPassword = string.Empty;

    public ChangePasswordViewModel(IApiClient api, INavigationService nav)
    {
        _api = api;
        _nav = nav;
    }

    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task ChangeAsync()
    {
        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp";
            return;
        }

        ClearError();
        IsBusy = true;
        try
        {
            var result = await _api.PostAsync<object, object>(
                "/api/v1/auth/change-password",
                new { newPassword = NewPassword });

            if (result?.Success == true)
                _nav.NavigateTo<MainViewModel>();
            else
                ErrorMessage = result?.Error ?? "Đổi mật khẩu thất bại";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanChange() =>
        !string.IsNullOrWhiteSpace(NewPassword) && !string.IsNullOrWhiteSpace(ConfirmPassword);
}
