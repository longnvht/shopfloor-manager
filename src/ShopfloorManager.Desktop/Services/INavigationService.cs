namespace ShopfloorManager.Desktop.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateBack();
}
