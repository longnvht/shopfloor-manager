using System.Windows.Data;
using System.Windows.Markup;

namespace ShopfloorManager.Desktop.Localization;

/// <summary>
/// XAML: Text="{loc:Loc Key=Login_Title}" — bind tới LocalizationManager.Instance["Login_Title"],
/// tự refresh khi LocalizationManager.SetLanguage() được gọi (PropertyChanged "Item[]").
/// </summary>
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
