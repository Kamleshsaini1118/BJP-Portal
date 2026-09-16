using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp.Views.Reports
{
    public partial class OpeningClosingReportControl : UserControl
    {
        public class OpeningClosingRow
        {
            public string ItemName { get; set; } = "";
            public decimal OpeningStock { get; set; }
            public string OpeningStockDisplay => $"{OpeningStock:N0}";
            public decimal Received { get; set; }
            public string ReceivedDisplay => $"{Received:N0}";
            public decimal Dispatched { get; set; }
            public string DispatchedDisplay => $"{Dispatched:N0}";
            public decimal ClosingStock => OpeningStock + Received - Dispatched;
            public string ClosingStockDisplay => $"{ClosingStock:N0}";
        }

        private List<StockReportItem> _allStock = new();

        public OpeningClosingReportControl()
        {
            InitializeComponent();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var products = await ProductRepository.GetProductsAsync();
                var prodList = new List<string> { "Any item" };
                prodList.AddRange(products.Select(p => p.Name).OrderBy(n => n));
                ItemFilterCombo.ItemsSource = prodList;
                ItemFilterCombo.SelectedIndex = 0;

                _allStock = await StockReportRepository.GetReportAsync(null, null);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load opening & closing stock report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ItemFilterCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allStock == null) return;

            string? selectedItem = ItemFilterCombo.SelectedItem as string;
            if (selectedItem == "Any item") selectedItem = null;

            var filtered = _allStock.Where(s =>
            {
                if (!string.IsNullOrEmpty(selectedItem) && !s.Item.Equals(selectedItem, StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            }).OrderBy(s => s.Item).ToList();

            var rows = filtered.Select(s => new OpeningClosingRow
            {
                ItemName = s.Item,
                OpeningStock = 0,
                Received = s.TotalQty,
                Dispatched = s.Dispatched,
            }).ToList();

            StockGrid.ItemsSource = rows;
            EmptyGridText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totOpening = rows.Sum(r => r.OpeningStock);
            decimal totRec = rows.Sum(r => r.Received);
            decimal totDisp = rows.Sum(r => r.Dispatched);
            decimal totClose = rows.Sum(r => r.ClosingStock);

            SummaryBadgeText.Text = $"{rows.Count} items · Opening: {totOpening:N0} · Received: {totRec:N0} · Dispatched: {totDisp:N0} · Closing: {totClose:N0}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = StockGrid.ItemsSource as List<OpeningClosingRow>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(s => new List<string>
            {
                s.ItemName,
                s.OpeningStockDisplay,
                s.ReceivedDisplay,
                s.DispatchedDisplay,
                s.ClosingStockDisplay
            }).ToList();

            string fileName = $"opening-closing-stock-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Opening & Closing Stock Report",
                new List<string> { "Item", "Opening Stock", "Received", "Dispatched", "Closing Stock" }, rows);
        }
    }
}
