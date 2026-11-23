using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StockPortalApp.Helpers
{
    public static class ExcelExportHelper
    {
        /// <summary>
        /// Prompts the user for a save location, then writes a single-sheet .xlsx file
        /// with a bold header row followed by the given rows. Shows a friendly message
        /// if there's nothing to export, and a confirmation once the file is saved.
        /// </summary>
        public static void Export(string suggestedFileName, string sheetName, List<string> headers, List<List<string>> rows)
        {
            if (rows.Count == 0)
            {
                MessageBox.Show("There's nothing to export yet — try clearing filters or search first.",
                    "Nothing to export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = suggestedFileName,
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add(sheetName);

                for (int c = 0; c < headers.Count; c++)
                {
                    ws.Cell(1, c + 1).Value = headers[c];
                }
                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FBF5EE");

                for (int r = 0; r < rows.Count; r++)
                {
                    for (int c = 0; c < rows[r].Count; c++)
                    {
                        ws.Cell(r + 2, c + 1).Value = rows[r][c];
                    }
                }

                ws.Columns().AdjustToContents();
                ws.SheetView.FreezeRows(1);

                workbook.SaveAs(dialog.FileName);

                MessageBox.Show($"Exported {rows.Count} row(s) to:\n{dialog.FileName}", "Export complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not save the Excel file.\n\n{ex.Message}", "Export failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Calls the Web API export endpoint, streams the generated .xlsx file, and saves it
        /// to the user's chosen location with an optional prompt to open it.
        /// </summary>
        public static async Task ExportViaApiAsync(string suggestedFileName, string apiEndpoint)
        {
            var dialog = new SaveFileDialog
            {
                FileName = suggestedFileName,
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                bool success = await StockPortalApp.Services.ApiClient.DownloadFileAsync(apiEndpoint, dialog.FileName);
                if (success)
                {
                    var result = MessageBox.Show(
                        $"Report exported successfully!\n\nSaved to: {dialog.FileName}\n\nWould you like to open it now?",
                        "Export Complete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                    }
                }
                else
                {
                    MessageBox.Show("Failed to download export from the server. Check if there are records to export.",
                        "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not export file via API.\n\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
