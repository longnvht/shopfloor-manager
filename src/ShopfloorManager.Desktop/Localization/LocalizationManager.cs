using System.ComponentModel;
using System.Globalization;
using ShopfloorManager.Desktop.Resources;

namespace ShopfloorManager.Desktop.Localization;

/// <summary>
/// Singleton — cho phép đổi ngôn ngữ runtime không cần restart app.
/// XAML bind qua indexer: {Binding [Key], Source={x:Static loc:LocalizationManager.Instance}}
/// (hoặc dùng {loc:Loc Key=...} markup extension).
/// </summary>
public class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationManager() { }

    public string CurrentLanguage { get; private set; } = "vi";

    public void SetLanguage(string lang)
    {
        CurrentLanguage = lang == "en" ? "en" : "vi";
        var culture = CurrentLanguage == "en" ? new CultureInfo("en-US") : new CultureInfo("vi-VN");

        Strings.Culture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        // "Item[]" notifies all indexer bindings to refresh
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string this[string key] => Strings.GetString(key) ?? key;
}
