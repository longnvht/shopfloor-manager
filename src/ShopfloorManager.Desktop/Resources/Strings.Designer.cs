using System.Globalization;
using System.Resources;

namespace ShopfloorManager.Desktop.Resources;

/// <summary>
/// Strongly-typed access to Strings.resx (vi = neutral/default) and Strings.en-US.resx (satellite).
/// Hand-written instead of VS-generated — keeps the indexer-based <see cref="Localization.LocalizationManager"/>
/// pattern simple without requiring the ResX single-file generator.
/// </summary>
internal static class Strings
{
    private static ResourceManager? _resourceManager;

    internal static ResourceManager ResourceManager =>
        _resourceManager ??= new ResourceManager("ShopfloorManager.Desktop.Resources.Strings", typeof(Strings).Assembly);

    internal static CultureInfo? Culture { get; set; }

    internal static string? GetString(string name) => ResourceManager.GetString(name, Culture);
}
