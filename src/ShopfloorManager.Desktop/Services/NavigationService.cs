using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ShopfloorManager.Desktop.ViewModels.Base;

namespace ShopfloorManager.Desktop.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _sp;
    private readonly Stack<ViewModelBase> _history = new();

    public ViewModelBase? CurrentViewModel { get; private set; }
    public event Action<ViewModelBase>? Navigated;

    public NavigationService(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var vm = _sp.GetRequiredService<TViewModel>() as ViewModelBase
            ?? throw new InvalidOperationException($"{typeof(TViewModel).Name} must extend ViewModelBase");

        if (CurrentViewModel is not null)
            _history.Push(CurrentViewModel);

        CurrentViewModel = vm;
        Navigated?.Invoke(vm);
    }

    public void NavigateBack()
    {
        if (_history.Count == 0) return;
        CurrentViewModel = _history.Pop();
        Navigated?.Invoke(CurrentViewModel);
    }
}
