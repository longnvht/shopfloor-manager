using System.Windows.Input;

namespace ShopfloorManager.Desktop.Models;

public record ShortcutItem(
    string Title,
    string IconKind,       // MaterialDesign PackIconKind name
    ICommand Command,
    bool IsEnabled = true,
    string? Badge = null);
