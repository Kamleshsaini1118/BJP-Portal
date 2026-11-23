using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace StockPortal.API.Services
{
    public interface IExcelService
    {
        byte[] CreateExcel<T>(
            string sheetName,
            IEnumerable<T> data,
            Dictionary<string, Func<T, object?>> columnMappings);
    }

    public class ExcelService : IExcelService
    {
        public byte[] CreateExcel<T>(
            string sheetName,
            IEnumerable<T> data,
            Dictionary<string, Func<T, object?>> columnMappings)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                throw new ArgumentException("Worksheet name is required.", nameof(sheetName));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (columnMappings == null)
            {
                throw new ArgumentNullException(nameof(columnMappings));
            }

            if (columnMappings.Count == 0)
            {
                throw new ArgumentException("At least one column mapping is required.", nameof(columnMappings));
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            int columnIndex = 1;
            foreach (var header in columnMappings.Keys)
            {
                worksheet.Cell(1, columnIndex++).Value = header;
            }

            worksheet.Row(1).Style.Font.Bold = true;
            worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FBF5EE");

            int rowIndex = 2;
            foreach (var item in data)
            {
                if (item == null)
                {
                    throw new ArgumentException("Data cannot contain null items.", nameof(data));
                }

                columnIndex = 1;
                foreach (var mapping in columnMappings.Values)
                {
                    var value = mapping(item);
                    worksheet.Cell(rowIndex, columnIndex++).Value = value?.ToString() ?? string.Empty;
                }

                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}