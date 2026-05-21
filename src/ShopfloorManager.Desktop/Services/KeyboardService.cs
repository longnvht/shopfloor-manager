using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShopfloorManager.Desktop.Behaviors;
using ShopfloorManager.Desktop.Controls;

namespace ShopfloorManager.Desktop.Services;

public class KeyboardService : IKeyboardService
{
    private TextBox? _activeTextBox;
    private PasswordBox? _activePasswordBox;
    private NumPadWindow? _numPad;
    private QwertyWindow? _qwerty;

    public void Initialize()
    {
        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.GotFocusEvent,
            new RoutedEventHandler(OnTextBoxGotFocus));

        EventManager.RegisterClassHandler(typeof(PasswordBox),
            UIElement.GotFocusEvent,
            new RoutedEventHandler(OnPasswordBoxGotFocus));

        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.LostFocusEvent,
            new RoutedEventHandler(OnInputLostFocus));

        EventManager.RegisterClassHandler(typeof(PasswordBox),
            UIElement.LostFocusEvent,
            new RoutedEventHandler(OnInputLostFocus));
    }

    private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        _activeTextBox = tb;
        _activePasswordBox = null;

        var mode = KeyboardBehavior.GetMode(tb);
        if (mode == KeyboardMode.None) { Hide(); return; }

        if (mode == KeyboardMode.NumPad)
            ShowNumPad();
        else
            ShowQwerty();
    }

    private void OnPasswordBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox pb) return;
        _activePasswordBox = pb;
        _activeTextBox = null;
        ShowQwerty();
    }

    private void OnInputLostFocus(object sender, RoutedEventArgs e)
    {
        // Delay để tránh hide khi click vào keyboard
        Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
        {
            var focused = Keyboard.FocusedElement;
            if (focused is TextBox or PasswordBox) return;
            // Check nếu focus đang ở keyboard window thì không hide
        });
    }

    private void ShowNumPad()
    {
        _qwerty?.Hide();
        if (_numPad == null || !_numPad.IsLoaded)
        {
            _numPad = new NumPadWindow(this);
            _numPad.Show();
        }
        else
        {
            _numPad.Visibility = Visibility.Visible;
        }
        _numPad.UpdateDisplay(GetCurrentValue());
    }

    private void ShowQwerty()
    {
        _numPad?.Hide();
        if (_qwerty == null || !_qwerty.IsLoaded)
        {
            _qwerty = new QwertyWindow(this);
            _qwerty.Show();
        }
        else
        {
            _qwerty.Visibility = Visibility.Visible;
        }
    }

    public void ShowFor(TextBox textBox, KeyboardMode mode)
    {
        _activeTextBox = textBox;
        _activePasswordBox = null;
        if (mode == KeyboardMode.NumPad) ShowNumPad();
        else ShowQwerty();
    }

    public void ShowFor(PasswordBox passwordBox)
    {
        _activePasswordBox = passwordBox;
        _activeTextBox = null;
        ShowQwerty();
    }

    public void Hide()
    {
        _numPad?.Hide();
        _qwerty?.Hide();
    }

    public void InsertText(string text)
    {
        if (_activeTextBox is not null)
        {
            var start = _activeTextBox.SelectionStart;
            _activeTextBox.Text = _activeTextBox.Text.Insert(start, text);
            _activeTextBox.SelectionStart = start + text.Length;
            _numPad?.UpdateDisplay(_activeTextBox.Text);
        }
        else if (_activePasswordBox is not null)
        {
            _activePasswordBox.Password += text;
        }
    }

    public void Backspace()
    {
        if (_activeTextBox is not null && _activeTextBox.Text.Length > 0)
        {
            var start = _activeTextBox.SelectionStart;
            if (start > 0)
            {
                _activeTextBox.Text = _activeTextBox.Text.Remove(start - 1, 1);
                _activeTextBox.SelectionStart = start - 1;
                _numPad?.UpdateDisplay(_activeTextBox.Text);
            }
        }
        else if (_activePasswordBox is not null && _activePasswordBox.Password.Length > 0)
        {
            _activePasswordBox.Password = _activePasswordBox.Password[..^1];
        }
    }

    public void Clear()
    {
        if (_activeTextBox is not null)
        {
            _activeTextBox.Text = string.Empty;
            _numPad?.UpdateDisplay(string.Empty);
        }
        else if (_activePasswordBox is not null)
        {
            _activePasswordBox.Password = string.Empty;
        }
    }

    private string GetCurrentValue() =>
        _activeTextBox?.Text ?? string.Empty;
}
