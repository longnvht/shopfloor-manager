using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopfloorManager.Desktop.Configuration;
using ShopfloorManager.Desktop.Services;
using ShopfloorManager.Desktop.ViewModels;
using ShopfloorManager.Desktop.ViewModels.Base;
using ShopfloorManager.Desktop.Views;

namespace ShopfloorManager.Desktop;

public partial class App : Application
{
    private ServiceProvider _sp = null!;
    private Window? _currentWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("local.json", optional: true)
            .Build();

        var settings = config.Get<AppSettings>() ?? new AppSettings();

        var services = new ServiceCollection();
        ConfigureServices(services, settings);
        _sp = services.BuildServiceProvider();

        var nav = _sp.GetRequiredService<NavigationService>();
        nav.Navigated += OnNavigated;

        var loginWindow = _sp.GetRequiredService<LoginWindow>();
        _currentWindow = loginWindow;
        loginWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services, AppSettings settings)
    {
        // Config
        services.AddSingleton(settings);

        // HTTP
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Services (singleton so token persists across navigations)
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());

        // ViewModels (transient — fresh instance each navigation)
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<JobListViewModel>();

        // Views
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();

        // ServiceProvider self-registration (cho MainViewModel dùng)
        services.AddSingleton<IServiceProvider>(sp => sp);
    }

    private void OnNavigated(ViewModelBase vm)
    {
        Window? next = vm switch
        {
            MainViewModel => _sp.GetRequiredService<MainWindow>(),
            _ => null
        };

        if (next is null) return;

        next.Show();
        _currentWindow?.Close();
        _currentWindow = next;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _sp.Dispose();
        base.OnExit(e);
    }
}
