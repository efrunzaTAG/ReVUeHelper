using System;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace ReVUeHelper_Explore
{
    public static class ExcelHelper
    {
        const string DefaultAssetColumn = "Property ID";

        public static List<long> ReadAssetIds(string path, string columnName = DefaultAssetColumn)
        {
            var ids = new List<long>();
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            var headerRow = ws.FirstRowUsed() ?? throw new Exception("Worksheet is empty");
            int assetCol = FindColumn(headerRow, columnName);
            int firstDataRow = headerRow.RowNumber() + 1;
            int lastRow      = ws.LastRowUsed()!.RowNumber();
            int totalDataRows = lastRow - firstDataRow + 1;

            int blank = 0, unparsed = 0;
            for (int r = firstDataRow; r <= lastRow; r++)
            {
                var cell = ws.Cell(r, assetCol);
                // Prefer numeric value when the cell is a number (avoids "8404981.0" / locale issues)
                if (cell.DataType == XLDataType.Number)
                {
                    ids.Add((long)cell.GetDouble());
                    continue;
                }

                var raw = cell.GetString().Trim();
                if (raw.Length == 0)
                {
                    blank++;
                    Console.WriteLine($"  [ReadAssetIds] row {r}: blank Property ID");
                    continue;
                }
                if (long.TryParse(raw, out long id))
                {
                    ids.Add(id);
                }
                else
                {
                    unparsed++;
                    Console.WriteLine($"  [ReadAssetIds] row {r}: unparseable Property ID = '{raw}'");
                }
            }
            Console.WriteLine($"  [ReadAssetIds] data rows: {totalDataRows}, parsed: {ids.Count}, blank: {blank}, unparseable: {unparsed}");
            return ids;
        }

        public static void WriteResults(
            string inputPath,
            string outputPath,
            Dictionary<long, long> assetToAsg,
            Dictionary<long, ConstellationRow> firstByAsg,
            Dictionary<long, (double Elevation, string Source)> elevationByAsg,
            string columnName = DefaultAssetColumn)
        {
            using var wb = new XLWorkbook(inputPath);
            var ws = wb.Worksheet(1);

            var headerRow = ws.FirstRowUsed() ?? throw new Exception("Worksheet is empty");
            int assetCol = FindColumn(headerRow, columnName);

            int lastCol = 0;
            foreach (var cell in headerRow.CellsUsed())
                if (cell.Address.ColumnNumber > lastCol) lastCol = cell.Address.ColumnNumber;

            int elevCol = lastCol + 1;
            int srcCol  = lastCol + 2;
            int latCol  = lastCol + 3;
            int lngCol  = lastCol + 4;
            int headerRowNum = headerRow.RowNumber();
            ws.Cell(headerRowNum, elevCol).Value = "Elevation";
            ws.Cell(headerRowNum, srcCol).Value  = "Source";
            ws.Cell(headerRowNum, latCol).Value  = "Latitude";
            ws.Cell(headerRowNum, lngCol).Value  = "Longitude";

            int lastRow = ws.LastRowUsed()!.RowNumber();
            for (int r = headerRowNum + 1; r <= lastRow; r++)
            {
                var raw = ws.Cell(r, assetCol).GetString().Trim();
                if (!long.TryParse(raw, out long assetId)) continue;
                if (!assetToAsg.TryGetValue(assetId, out long asgId)) continue;

                if (firstByAsg.TryGetValue(asgId, out var crow))
                {
                    if (crow.Latitude  != null) ws.Cell(r, latCol).Value = crow.Latitude.Value;
                    if (crow.Longitude != null) ws.Cell(r, lngCol).Value = crow.Longitude.Value;
                }

                if (elevationByAsg.TryGetValue(asgId, out var data))
                {
                    ws.Cell(r, elevCol).Value = data.Elevation;
                    ws.Cell(r, srcCol).Value  = data.Source;
                }
            }

            wb.SaveAs(outputPath);
        }

        static int FindColumn(IXLRow headerRow, string columnName)
        {
            foreach (var cell in headerRow.CellsUsed())
            {
                if (string.Equals(cell.GetString().Trim(), columnName, StringComparison.OrdinalIgnoreCase))
                    return cell.Address.ColumnNumber;
            }
            throw new Exception($"Column '{columnName}' not found in header row.");
        }
    }
}
