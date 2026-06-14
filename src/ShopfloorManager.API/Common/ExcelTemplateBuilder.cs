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
