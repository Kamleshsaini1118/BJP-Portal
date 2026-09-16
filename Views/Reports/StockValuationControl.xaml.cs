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
    public partial class StockValuationControl : UserControl
    {
        public class ValuationRow
        {
            public string ProductName { get; set; } = "";
            public decimal RemainingQty { get; set; }
            public string RemainingQtyDisplay => $"{RemainingQty:N0}";
            public decimal? LastPurchasePrice { get; set; }
            public string LastPurchasePriceDisplay => LastPurchasePrice.HasValue && LastPurchasePrice.Value > 0 ? $"₹{LastPurchasePrice.Value:N0}" : "—";
            public decimal? StockValue => LastPurchasePrice.HasValue ? RemainingQty * LastPurchasePrice.Value : null;
            public string StockValueDisplay => StockValue.HasValue && StockValue.Value > 0 ? $"₹{StockValue.Value:N0}" : "—";
        }

        private List<ValuationRow> _allValuations = new();

        public StockValuationControl()
        {
            InitializeComponent();
            NumericInputHelper.AttachDecimalOnly(MinStockValueBox);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var products = await ProductRepository.GetProductsAsync();
                var prodList = new List<string> { "Any item" };
                prodList.AddRange(products.Select(p => p.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n));
                ItemFilterCombo.ItemsSource = prodList;
                ItemFilterCombo.SelectedIndex = 0;

                var stockReport = await StockReportRepository.GetReportAsync(null, null);
                var prodDict = products
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                _allValuations = stockReport.Select(s =>
                {
                    decimal? lastPrice = null;
                    if (prodDict.TryGetValue(s.Item, out var prod) && prod.AvgPrice > 0)
                    {
                        lastPrice = prod.AvgPrice;
                    }
                    return new ValuationRow
                    {
                        ProductName = s.Item,
                        RemainingQty = s.Remaining,
                        LastPurchasePrice = lastPrice
                    };
                }).OrderByDescending(v => v.StockValue ?? 0).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load stock valuation report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ItemFilterCombo.SelectedIndex = 0;
            MinStockValueBox.Text = "";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allValuations == null) return;

            string? selectedItem = ItemFilterCombo.SelectedItem as string;
            if (selectedItem == "Any item") selectedItem = null;

            decimal? minVal = decimal.TryParse(MinStockValueBox.Text.Trim(), out var val) ? val : null;

            var filtered = _allValuations.Where(v =>
            {
                if (!string.IsNullOrEmpty(selectedItem) && !v.ProductName.Equals(selectedItem, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (minVal.HasValue && (v.StockValue ?? 0) < minVal.Value)
                    return false;

                return true;
            }).ToList();

            ValuationGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalStockVal = filtered.Sum(v => v.StockValue ?? 0);
            SummaryBadgeText.Text = $"{filtered.Count} items · Total Stock Value: ₹{totalStockVal:N0}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = ValuationGrid.ItemsSource as List<ValuationRow>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(v => new List<string>
            {
                v.ProductName,
                v.RemainingQtyDisplay,
                v.LastPurchasePriceDisplay,
                v.StockValueDisplay
            }).ToList();

            string fileName = $"stock-valuation-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Stock Valuation Report",
                new List<string> { "Item", "Remaining Qty", "Last Purchase Price", "Stock Value" }, rows);
        }
    }
}
