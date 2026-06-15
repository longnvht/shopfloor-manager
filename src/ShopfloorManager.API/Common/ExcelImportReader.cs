using ClosedXML.Excel;

namespace ShopfloorManager.API.Common;

/// <summary>
/// Đọc file Excel import: sheet đầu tiên, dòng 1 = header.
/// Header được normalize (lower, trim, bỏ space) để match tên cột linh hoạt.
/// </summary>
public static class ExcelImportReader
{
    public static (Dictionary<string, int> Headers, List<Dictionary<string, string>> Rows) Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var usedRows = sheet.RowsUsed().ToList();

        var headers = new Dictionary<string, int>();
        if (usedRows.Count > 0)
        {
            foreach (var cell in usedRows[0].CellsUsed())
            {
                var key = Normalize(cell.GetString());
                if (!string.IsNullOrEmpty(key))
                    headers[key] = cell.Address.ColumnNumber;
            }
        }

        var rows = new List<Dictionary<string, string>>();
        foreach (var row in usedRows.Skip(1))
        {
            var values = new Dictionary<string, string>();
            foreach (var (name, col) in headers)
            {
                var value = row.Cell(col).GetString().Trim();
                if (!string.IsNullOrEmpty(value))
                    values[name] = value;
            }
            rows.Add(values);
        }

        return (headers, rows);
    }

    public static string? Cell(Dictionary<string, string> row, string name) =>
        row.TryGetValue(Normalize(name), out var value) ? value : null;

    public static decimal? CellDecimal(Dictionary<string, string> row, string name)
    {
        var raw = Cell(row, name);
        return raw != null && decimal.TryParse(raw, out var value) ? value : null;
    }

    /// <summary>Thử nhiều tên cột — trả về giá trị của cột đầu tiên match.</summary>
    public static string? Cell(Dictionary<string, string> row, params string[] names)
    {
        foreach (var name in names)
        {
            var value = Cell(row, name);
            if (value != null) return value;
        }
        return null;
    }

    public static decimal? CellDecimal(Dictionary<string, string> row, params string[] names)
    {
        var raw = Cell(row, names);
        return raw != null && decimal.TryParse(raw, out var value) ? value : null;
    }

    public static int? CellInt(Dictionary<string, string> row, params string[] names)
    {
        var raw = Cell(row, names);
        return raw != null && int.TryParse(raw, out var value) ? value : null;
    }

    /// <summary>Đọc ô ngày — hỗ trợ cả chuỗi ngày (yyyy-MM-dd, dd/MM/yyyy...) và Excel serial date number.</summary>
    public static DateOnly? CellDate(Dictionary<string, string> row, params string[] names)
    {
        var raw = Cell(row, names);
        if (raw is null) return null;
        if (DateOnly.TryParse(raw, out var date)) return date;
        if (double.TryParse(raw, out var oaDate))
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(oaDate)); }
            catch (ArgumentException) { return null; }
        }
        return null;
    }

    private static string Normalize(string s) => s.Trim().ToLowerInvariant().Replace(" ", "");
}
