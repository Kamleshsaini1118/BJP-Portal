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
    public partial class VendorSummaryControl : UserControl
    {
        public class VendorSummaryRow
        {
            public string Name { get; set; } = "";
            public int TotalPurchasesCount { get; set; }
            public decimal TotalSpend { get; set; }
            public string TotalSpendDisplay => $"₹{TotalSpend:N1}";
            public DateTime? LastPurchaseDate { get; set; }
            public string LastPurchaseDateDisplay => LastPurchaseDate.HasValue ? LastPurchaseDate.Value.ToString("dd MMM yyyy") : "—";
        }

        private List<Vendor> _allVendors = new();
        private List<Purchase> _allPurchases = new();

        public VendorSummaryControl()
        {
            InitializeComponent();
            NumericInputHelper.AttachDecimalOnly(MinAmountBox);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                _allVendors = await VendorRepository.GetVendorsAsync();
                _allPurchases = await PurchaseRepository.GetPurchasesAsync(null, null, null, null);

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load vendor summary data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            MinAmountBox.Text = "";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allVendors == null || _allPurchases == null) return;

            DateTime? fromDate = FromDatePicker.SelectedDate?.Date;
            DateTime? toDate = ToDatePicker.SelectedDate?.Date;
            decimal? minAmt = decimal.TryParse(MinAmountBox.Text.Trim(), out var val) ? val : null;

            var filteredPurchases = _allPurchases.Where(p =>
            {
                if (fromDate.HasValue && p.PurchaseDate.Date < fromDate.Value)
                    return false;

                if (toDate.HasValue && p.PurchaseDate.Date > toDate.Value)
                    return false;

                return true;
            }).ToList();

            var rows = _allVendors.Select(v =>
            {
                var vPurchases = filteredPurchases.Where(p => p.VendorName.Equals(v.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                DateTime? lastDate = vPurchases.Count > 0 ? vPurchases.Max(p => p.PurchaseDate) : null;

                return new VendorSummaryRow
                {
                    Name = v.Name,
                    TotalPurchasesCount = vPurchases.Count,
                    TotalSpend = vPurchases.Sum(p => p.Total),
                    LastPurchaseDate = lastDate
                };
            }).Where(r => r.TotalPurchasesCount > 0 && (!minAmt.HasValue || r.TotalSpend >= minAmt.Value))
              .OrderByDescending(r => r.TotalSpend).ToList();

            VendorGrid.ItemsSource = rows;
            EmptyGridText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal grandTotal = rows.Sum(r => r.TotalSpend);
            SummaryBadgeText.Text = $"{rows.Count} vendors · Grand Total: ₹{grandTotal:N0}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = VendorGrid.ItemsSource as List<VendorSummaryRow>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(r => new List<string>
            {
                r.Name,
                r.TotalPurchasesCount.ToString(),
                r.TotalSpendDisplay,
                r.LastPurchaseDateDisplay
            }).ToList();

            string fileName = $"vendor-summary-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Vendor Summary Report",
                new List<string> { "Vendor", "Purchases", "Total Amount", "Last Purchase Date" }, rows);
        }
    }
}
