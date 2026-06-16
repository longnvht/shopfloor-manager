using ClosedXML.Excel;

namespace ShopfloorManager.API.Common;

public static class ExcelTemplateBuilder
{
    public static byte[] BuildOpsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Operations");

        string[] headers = ["OpNumber", "OpType", "Description", "SetupTime", "ProdTime"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        ws.Cell(2, 1).Value = "10";
        ws.Cell(2, 2).Value = "CNC";
        ws.Cell(2, 3).Value = "Phay mặt đầu";
        ws.Cell(2, 4).Value = 30;
        ws.Cell(2, 5).Value = 5;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildJobBatchTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Jobs");

        string[] headers =
        [
            "PartNumber", "PartDescription", "Revision", "JobNumber", "PONumber", "POLine",
            "RunQty", "ShipBy", "OpNumber", "OpType", "OpDescription", "SetupTime", "ProdTime"
        ];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        ws.Cell(2, 1).Value = "SHAFT-50H6";
        ws.Cell(2, 2).Value = "Shaft 50mm";
        ws.Cell(2, 3).Value = "A";
        ws.Cell(2, 4).Value = "J2026-001";
        ws.Cell(2, 5).Value = "PO-1001";
        ws.Cell(2, 6).Value = "1";
        ws.Cell(2, 7).Value = 10;
        ws.Cell(2, 8).Value = new DateTime(2026, 7, 1);
        ws.Cell(2, 8).Style.DateFormat.Format = "yyyy-mm-dd";
        ws.Cell(2, 9).Value = "10";
        ws.Cell(2, 10).Value = "CNC";
        ws.Cell(2, 11).Value = "Phay mặt đầu";
        ws.Cell(2, 12).Value = 30;
        ws.Cell(2, 13).Value = 5;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>File Excel mẫu có đủ dữ liệu thực (3 Part, 3 Job, 9 OP) để test bulk import.</summary>
    public static byte[] BuildJobBatchSampleData()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Jobs");

        string[] headers =
        [
            "PartNumber", "PartDescription", "Revision", "JobNumber", "PONumber", "POLine",
            "RunQty", "ShipBy", "OpNumber", "OpType", "OpDescription", "SetupTime", "ProdTime"
        ];
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        object[][] rows =
        [
            // Job 1: HOUSING-AL6061 / Rev A / J2026-S01 — 3 OP
            ["HOUSING-AL6061", "Housing nhôm AL6061",  "A", "J2026-S01", "PO-2001", "1", 5,  new DateTime(2026, 7, 15), "10", "CNC",   "Phay mặt đầu + khoan lỗ Ø12", 45, 8],
            ["HOUSING-AL6061", "Housing nhôm AL6061",  "A", "J2026-S01", "PO-2001", "1", 5,  new DateTime(2026, 7, 15), "20", "GRIND", "Mài bề mặt đạt Ra0.8",        30, 4],
            ["HOUSING-AL6061", "Housing nhôm AL6061",  "A", "J2026-S01", "PO-2001", "1", 5,  new DateTime(2026, 7, 15), "30", "THD",   "Taro M6×1 (4 lỗ)",            20, 3],
            // Job 2: BRACKET-ST304 / Rev B / J2026-S02 — 4 OP
            ["BRACKET-ST304",  "Bracket thép 304",     "B", "J2026-S02", "PO-2002", "1", 10, new DateTime(2026, 7, 20), "10", "CNC",   "Phay profile ngoài",          60, 12],
            ["BRACKET-ST304",  "Bracket thép 304",     "B", "J2026-S02", "PO-2002", "1", 10, new DateTime(2026, 7, 20), "20", "CNC",   "Khoan taro M8×1.25",          40, 6],
            ["BRACKET-ST304",  "Bracket thép 304",     "B", "J2026-S02", "PO-2002", "1", 10, new DateTime(2026, 7, 20), "30", "GRIND", "Mài phẳng đạt Ra1.6",         25, 3],
            ["BRACKET-ST304",  "Bracket thép 304",     "B", "J2026-S02", "PO-2002", "1", 10, new DateTime(2026, 7, 20), "40", "WDP",   "Kiểm tra cuối + đo kiểm",     10, 2],
            // Job 3: SHAFT-16H7 / Rev A / J2026-S03 — 2 OP
            ["SHAFT-16H7",     "Trục Ø16 H7",          "A", "J2026-S03", "PO-2003", "1", 20, new DateTime(2026, 8, 1),  "10", "CNC",   "Tiện ngoài Ø16.05",           35, 6],
            ["SHAFT-16H7",     "Trục Ø16 H7",          "A", "J2026-S03", "PO-2003", "1", 20, new DateTime(2026, 8, 1),  "20", "GRIND", "Mài tròn ngoài đạt H7",       45, 5],
        ];

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var cell = ws.Cell(r + 2, c + 1);
                if (rows[r][c] is DateTime dt)
                {
                    cell.Value = dt;
                    cell.Style.DateFormat.Format = "yyyy-mm-dd";
                }
                else
                {
                    cell.Value = XLCellValue.FromObject(rows[r][c]);
                }
            }
        }

        ws.Columns().AdjustToContents();
        // Highlight header row
        ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#4A2512");
        ws.Range(1, 1, 1, headers.Length).Style.Font.FontColor = XLColor.White;

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildDimensionsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Dimensions");

        string[] headers = ["BalloonNumber", "Code", "Description", "Nominal", "TolPlus", "TolMinus", "Unit", "Category"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

        ws.Cell(2, 1).Value = "Ø1";
        ws.Cell(2, 2).Value = "D1";
        ws.Cell(2, 3).Value = "Đường kính ngoài";
        ws.Cell(2, 4).Value = 25.4;
        ws.Cell(2, 5).Value = 0.05;
        ws.Cell(2, 6).Value = 0.05;
        ws.Cell(2, 7).Value = "mm";
        ws.Cell(2, 8).Value = "LIN";

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
