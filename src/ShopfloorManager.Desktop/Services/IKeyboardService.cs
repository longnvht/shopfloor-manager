using System.Windows.Controls;
using ShopfloorManager.Desktop.Behaviors;

namespace ShopfloorManager.Desktop.Services;

public interface IKeyboardService
{
    void Initialize();
    void ShowFor(TextBox textBox, KeyboardMode mode);
    void ShowFor(PasswordBox passwordBox);
    void Hide();
    void InsertText(string text);
    void Backspace();
    void Clear();
}
