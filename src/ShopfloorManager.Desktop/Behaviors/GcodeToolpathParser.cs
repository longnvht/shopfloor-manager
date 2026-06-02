using System.Text.RegularExpressions;

namespace ShopfloorManager.Desktop.Behaviors;

public enum MoveType { Rapid, Linear, ArcCW, ArcCCW }

/// <summary>Một đoạn di chuyển trong toolpath (2D, mặt phẳng XY).</summary>
public record ToolpathMove(
    double X1, double Y1,
    double X2, double Y2,
    MoveType Type,
    double CX = 0, double CY = 0);   // tâm cung (chỉ dùng cho Arc)

/// <summary>Parse G-code text → List&lt;ToolpathMove&gt; cho visualizer.</summary>
public static class GcodeToolpathParser
{
    private static readonly Regex WordRx =
        new(@"([A-Za-z])([+-]?[\d]*\.?[\d]+)", RegexOptions.Compiled);

    public static List<ToolpathMove> Parse(string gcode)
    {
        var moves = new List<ToolpathMove>();
        if (string.IsNullOrWhiteSpace(gcode)) return moves;

        double curX = 0, curY = 0, curZ = 0;
        bool absolute   = true;
        int  modal      = 0;   // 0=G0 rapid, 1=G1 linear, 2=G2 CW arc, 3=G3 CCW arc

        foreach (var rawLine in gcode.Split('\n'))
        {
            var line = StripComments(rawLine).Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var words = ParseWords(line);
            if (words.Count == 0) continue;

            // Cập nhật trạng thái modal
            foreach (var (l, v) in words)
            {
                if (l != 'G') continue;
                int g = (int)Math.Round(v);
                switch (g)
                {
                    case 90: absolute = true;  break;
                    case 91: absolute = false; break;
                    case 0: case 1: case 2: case 3: modal = g; break;
                }
            }

            bool hasX = words.Any(w => w.L == 'X');
            bool hasY = words.Any(w => w.L == 'Y');
            bool hasZ = words.Any(w => w.L == 'Z');
            if (!hasX && !hasY && !hasZ) continue;

            double tX = Resolve(words, 'X', curX, absolute);
            double tY = Resolve(words, 'Y', curY, absolute);
            double tZ = Resolve(words, 'Z', curZ, absolute);

            if (Math.Abs(tX - curX) > 1e-9 || Math.Abs(tY - curY) > 1e-9)
            {
                ToolpathMove? move = modal switch
                {
                    2 or 3 => BuildArc(curX, curY, tX, tY, words, modal),
                    1      => new ToolpathMove(curX, curY, tX, tY, MoveType.Linear),
                    _      => new ToolpathMove(curX, curY, tX, tY, MoveType.Rapid),
                };

                if (move is not null)
                {
                    // Vòng tròn đầy (start == end): tách thành 2 nửa tránh degenerate arc
                    if (move.Type is MoveType.ArcCW or MoveType.ArcCCW
                        && Math.Abs(tX - curX) < 1e-6 && Math.Abs(tY - curY) < 1e-6)
                    {
                        var (mx, my) = ArcMidpoint(move);
                        moves.Add(move with { X2 = mx, Y2 = my });
                        moves.Add(move with { X1 = mx, Y1 = my });
                    }
                    else
                    {
                        moves.Add(move);
                    }
                }
            }

            curX = tX; curY = tY; curZ = tZ;
        }

        return moves;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static ToolpathMove? BuildArc(double x1, double y1, double x2, double y2,
        List<(char L, double V)> words, int gMode)
    {
        double i = Raw(words, 'I', 0);
        double j = Raw(words, 'J', 0);
        if (Math.Abs(i) < 1e-9 && Math.Abs(j) < 1e-9) return null;
        return new ToolpathMove(x1, y1, x2, y2,
            gMode == 2 ? MoveType.ArcCW : MoveType.ArcCCW,
            x1 + i, y1 + j);
    }

    private static (double x, double y) ArcMidpoint(ToolpathMove m)
    {
        double r     = Math.Sqrt(Math.Pow(m.X1 - m.CX, 2) + Math.Pow(m.Y1 - m.CY, 2));
        double start = Math.Atan2(m.Y1 - m.CY, m.X1 - m.CX);
        double mid   = m.Type == MoveType.ArcCW ? start - Math.PI : start + Math.PI;
        return (m.CX + r * Math.Cos(mid), m.CY + r * Math.Sin(mid));
    }

    /// <summary>Tính độ dài góc quét theo hướng G-code (CW cho G2, CCW cho G3).</summary>
    public static double ArcSpan(ToolpathMove m)
    {
        double s = Math.Atan2(m.Y1 - m.CY, m.X1 - m.CX);
        double e = Math.Atan2(m.Y2 - m.CY, m.X2 - m.CX);
        if (m.Type == MoveType.ArcCW)
        {
            double d = s - e; if (d <= 0) d += 2 * Math.PI; return d;
        }
        else
        {
            double d = e - s; if (d <= 0) d += 2 * Math.PI; return d;
        }
    }

    private static string StripComments(string line)
    {
        line = Regex.Replace(line, @"\([^)]*\)", "");
        int semi = line.IndexOf(';');
        return semi >= 0 ? line[..semi] : line;
    }

    private static List<(char L, double V)> ParseWords(string line)
    {
        var list = new List<(char, double)>();
        foreach (Match m in WordRx.Matches(line))
            if (double.TryParse(m.Groups[2].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v))
                list.Add((char.ToUpper(m.Groups[1].Value[0]), v));
        return list;
    }

    private static double Resolve(List<(char L, double V)> w, char l, double cur, bool abs)
    {
        var found = w.FirstOrDefault(x => x.L == l);
        if (found == default) return cur;
        return abs ? found.V : cur + found.V;
    }

    private static double Raw(List<(char L, double V)> w, char l, double def)
    {
        var found = w.FirstOrDefault(x => x.L == l);
        return found == default ? def : found.V;
    }
}
