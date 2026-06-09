using System;
using System.Data;
using System.Linq;
using ClosedXML.Excel;

namespace SQLdata_Generator.Services
{
    public class ExcelService : IExcelService
    {
        public DataTable ReadExcel(string filePath)
        {
            var dt = new DataTable();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var range = worksheet.RangeUsed();
            if (range == null) return dt;

            var firstRow = range.Row(1);
            foreach (var cell in firstRow.Cells())
            {
                dt.Columns.Add(cell.GetValue<string>().Trim(), typeof(string));
            }

            for (int r = 2; r <= range.RowCount(); r++)
            {
                var row = dt.NewRow();
                var rowUsed = worksheet.Row(r);
                var lastCol = rowUsed.LastCellUsed()?.WorksheetColumn()?.ColumnNumber() ?? dt.Columns.Count;

                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    var cell = worksheet.Cell(r, c + 1);
                    row[c] = cell.IsEmpty() ? DBNull.Value : cell.GetValue<string>();
                }
                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
