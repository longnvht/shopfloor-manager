using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ShopfloorManager.Desktop.Behaviors;

/// <summary>
/// Attached property cho RichTextBox: nhận chuỗi G-code thô, render với syntax highlight.
/// Usage: kb:GcodeViewerBehavior.Text="{Binding GcodeText}"
/// </summary>
public static class GcodeViewerBehavior
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(GcodeViewerBehavior),
            new PropertyMetadata(null, OnTextChanged));

    public static string? GetText(DependencyObject obj) => (string?)obj.GetValue(TextProperty);
    public static void SetText(DependencyObject obj, string? value) => obj.SetValue(TextProperty, value);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox rtb) return;
        var text = e.NewValue as string ?? "";
        rtb.Document = BuildDocument(text);
    }

    private static FlowDocument BuildDocument(string text)
    {
        var doc = new FlowDocument
        {
            FontFamily   = new FontFamily("Consolas, Courier New"),
            FontSize     = 13,
            PagePadding  = new Thickness(12, 8, 12, 8),
            // Very large finite value — tắt wrap cho code viewer (PositiveInfinity không valid trong WPF)
            PageWidth    = 100000.0,
            ColumnWidth  = 100000.0,
            Background   = Brushes.Transparent
        };

        var lines = text.Split('\n');
        // Limit to 5000 lines to avoid hanging on huge files
        int limit = Math.Min(lines.Length, 5000);
        for (int i = 0; i < limit; i++)
        {
            var para = new Paragraph { Margin = new Thickness(0), Padding = new Thickness(0) };
            ParseLine(lines[i].TrimEnd('\r'), para);
            doc.Blocks.Add(para);
        }

        if (lines.Length > 5000)
        {
            var notice = new Paragraph(new Run($"... [{lines.Length - 5000} dòng tiếp theo không hiển thị]")
            {
                Foreground = Brushes.Gray,
                FontStyle  = FontStyles.Italic
            })
            { Margin = new Thickness(0, 4, 0, 0) };
            doc.Blocks.Add(notice);
        }

        return doc;
    }

    private static void ParseLine(string line, Paragraph para)
    {
        if (string.IsNullOrEmpty(line))
        {
            para.Inlines.Add(new Run(" "));
            return;
        }

        // Split out comment: everything after ;
        int semiIdx   = line.IndexOf(';');
        string code   = semiIdx >= 0 ? line[..semiIdx] : line;
        string comment = semiIdx >= 0 ? line[semiIdx..] : "";

        // Handle block-comment ( ... ) that may span whole line
        if (code.TrimStart().StartsWith('('))
        {
            para.Inlines.Add(MakeRun(line, CommentColor));
            return;
        }

        // Tokenize code part by whitespace
        bool firstToken = true;
        foreach (var token in code.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!firstToken) para.Inlines.Add(new Run(" "));
            firstToken = false;

            // Inline comment token starting with (
            if (token.StartsWith('('))
            {
                para.Inlines.Add(MakeRun(token, CommentColor));
                continue;
            }

            if (token.Length == 0) continue;
            char c = char.ToUpperInvariant(token[0]);

            var color = c switch
            {
                'N'                     => NWordColor,
                'G'                     => GWordColor,
                'M'                     => MWordColor,
                'X' or 'Y' or 'Z'
                or 'A' or 'B' or 'C'
                or 'I' or 'J' or 'K'   => AxisColor,
                'F' or 'S'              => FeedColor,
                'T' or 'H' or 'D'      => ToolColor,
                'O'                     => ProgramColor,
                _                       => DefaultColor
            };

            para.Inlines.Add(MakeRun(token, color));
        }

        if (!string.IsNullOrEmpty(comment))
        {
            if (!firstToken) para.Inlines.Add(new Run(" "));
            para.Inlines.Add(MakeRun(comment, CommentColor));
        }
    }

    private static Run MakeRun(string text, Brush brush) =>
        new(text) { Foreground = brush };

    // Color palette
    private static readonly Brush NWordColor   = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));  // gray
    private static readonly Brush GWordColor   = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));  // blue
    private static readonly Brush MWordColor   = new SolidColorBrush(Color.FromRgb(0x6A, 0x1B, 0x9A));  // purple
    private static readonly Brush AxisColor    = new SolidColorBrush(Color.FromRgb(0xE6, 0x51, 0x00));  // orange (BrandAccent dark)
    private static readonly Brush FeedColor    = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));  // green
    private static readonly Brush ToolColor    = new SolidColorBrush(Color.FromRgb(0x00, 0x83, 0x8F));  // teal
    private static readonly Brush ProgramColor = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));  // red
    private static readonly Brush CommentColor = new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75));  // medium gray
    private static readonly Brush DefaultColor = new SolidColorBrush(Color.FromRgb(0x3E, 0x27, 0x23));  // BrandText
}
