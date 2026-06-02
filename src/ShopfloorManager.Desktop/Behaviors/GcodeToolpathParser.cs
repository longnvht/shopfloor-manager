using System.Text.RegularExpressions;

namespace ShopfloorManager.Desktop.Behaviors;

public enum MoveType { Rapid, Linear, ArcCW, ArcCCW }

public record ToolpathMove(
    double X1, double Y1,
    double X2, double Y2,
    MoveType Type,
    double CX = 0, double CY = 0);   // arc center (visualization space)

/// <summary>
/// Parse G-code → List&lt;ToolpathMove&gt; for 2D visualization.
/// Auto-detects machine type:
///   Turning (lathe) — Y never used, Z varies → plots X-Z plane (profile view)
///   Milling          — Y used               → plots X-Y plane (top-down view)
/// </summary>
public static class GcodeToolpathParser
{
    private static readonly Regex WordRx =
        new(@"([A-Za-z])([+-]?[\d]*\.?[\d]+)", RegexOptions.Compiled);

    // Full 3D raw move before axis-plane selection
    private record RawMove(
        double X1, double Y1, double Z1,
        double X2, double Y2, double Z2,
        int Modal,          // 0=G0 rapid, 1=G1 linear, 2=G2 CW arc, 3=G3 CCW arc
        double I, double J, double K);  // arc center offsets from start point

    public static List<ToolpathMove> Parse(string gcode)
    {
        if (string.IsNullOrWhiteSpace(gcode)) return [];

        var rawMoves = new List<RawMove>();

        double curX = 0, curY = 0, curZ = 0;
        bool absolute = true;
        int  modal    = 0;

        foreach (var rawLine in gcode.Split('\n'))
        {
            var line = StripComments(rawLine).Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var words = ParseWords(line);
            if (words.Count == 0) continue;

            foreach (var (l, v) in words)
            {
                if (l != 'G') continue;
                switch ((int)Math.Round(v))
                {
                    case 90: absolute = true;  break;
                    case 91: absolute = false; break;
                    case 0: case 1: case 2: case 3: modal = (int)Math.Round(v); break;
                }
            }

            bool hasX = words.Any(w => w.L == 'X');
            bool hasY = words.Any(w => w.L == 'Y');
            bool hasZ = words.Any(w => w.L == 'Z');
            if (!hasX && !hasY && !hasZ) continue;

            double tX = Resolve(words, 'X', curX, absolute);
            double tY = Resolve(words, 'Y', curY, absolute);
            double tZ = Resolve(words, 'Z', curZ, absolute);

            if (Math.Abs(tX - curX) > 1e-9 || Math.Abs(tY - curY) > 1e-9 || Math.Abs(tZ - curZ) > 1e-9)
            {
                rawMoves.Add(new RawMove(
                    curX, curY, curZ, tX, tY, tZ, modal,
                    Raw(words, 'I', 0), Raw(words, 'J', 0), Raw(words, 'K', 0)));
            }

            curX = tX; curY = tY; curZ = tZ;
        }

        if (rawMoves.Count == 0) return [];

        // Detect turning (lathe): Y is never used but Z varies.
        bool isTurning = rawMoves.All(m => Math.Abs(m.Y1) < 1e-9 && Math.Abs(m.Y2) < 1e-9)
                      && rawMoves.Any(m => Math.Abs(m.Z1 - m.Z2) > 1e-9);

        var result = new List<ToolpathMove>();
        foreach (var raw in rawMoves)
        {
            ToolpathMove? move;
            if (isTurning)
            {
                // X stays X; Z → visual Y. Arc center: I offset on X, K offset on Z.
                if (Math.Abs(raw.X1 - raw.X2) < 1e-9 && Math.Abs(raw.Z1 - raw.Z2) < 1e-9) continue;
                move = BuildMove(raw.X1, raw.Z1, raw.X2, raw.Z2, raw.Modal, raw.I, raw.K);
            }
            else
            {
                // Standard milling: X-Y plane. Arc center: I on X, J on Y.
                if (Math.Abs(raw.X1 - raw.X2) < 1e-9 && Math.Abs(raw.Y1 - raw.Y2) < 1e-9) continue;
                move = BuildMove(raw.X1, raw.Y1, raw.X2, raw.Y2, raw.Modal, raw.I, raw.J);
            }

            if (move is null) continue;

            // Full circle (start == end): split into 2 half-arcs to avoid degenerate geometry
            if (move.Type is MoveType.ArcCW or MoveType.ArcCCW
                && Math.Abs(move.X2 - move.X1) < 1e-6 && Math.Abs(move.Y2 - move.Y1) < 1e-6)
            {
                var (mx, my) = ArcMidpoint(move);
                result.Add(move with { X2 = mx, Y2 = my });
                result.Add(move with { X1 = mx, Y1 = my });
            }
            else
            {
                result.Add(move);
            }
        }

        return result;
    }

    private static ToolpathMove? BuildMove(
        double x1, double y1, double x2, double y2, int gMode, double i, double j)
    {
        return gMode switch
        {
            2 or 3 when (Math.Abs(i) > 1e-9 || Math.Abs(j) > 1e-9) =>
                new ToolpathMove(x1, y1, x2, y2,
                    gMode == 2 ? MoveType.ArcCW : MoveType.ArcCCW,
                    x1 + i, y1 + j),
            2 or 3 => null,   // no center data — skip degenerate arc
            1      => new ToolpathMove(x1, y1, x2, y2, MoveType.Linear),
            _      => new ToolpathMove(x1, y1, x2, y2, MoveType.Rapid),
        };
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static (double x, double y) ArcMidpoint(ToolpathMove m)
    {
        double r     = Math.Sqrt(Math.Pow(m.X1 - m.CX, 2) + Math.Pow(m.Y1 - m.CY, 2));
        double start = Math.Atan2(m.Y1 - m.CY, m.X1 - m.CX);
        double mid   = m.Type == MoveType.ArcCW ? start - Math.PI : start + Math.PI;
        return (m.CX + r * Math.Cos(mid), m.CY + r * Math.Sin(mid));
    }

    public static double ArcSpan(ToolpathMove m)
    {
        double s = Math.Atan2(m.Y1 - m.CY, m.X1 - m.CX);
        double e = Math.Atan2(m.Y2 - m.CY, m.X2 - m.CX);
        if (m.Type == MoveType.ArcCW)
        { double d = s - e; if (d <= 0) d += 2 * Math.PI; return d; }
        else
        { double d = e - s; if (d <= 0) d += 2 * Math.PI; return d; }
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
