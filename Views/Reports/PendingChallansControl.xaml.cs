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
    public partial class PendingChallansControl : UserControl
    {
        public class PendingChallanRow
        {
            public int Id { get; set; }
            public string ChallanNumber { get; set; } = "—";
            public string VendorName { get; set; } = "";
            public DateTime Date { get; set; }
            public string DateDisplay => Date.ToString("dd MMM yyyy");
            public int DaysPending => (IndiaTime.Today - Date.Date).Days;
            public string DaysPendingDisplay => $"{Math.Max(0, DaysPending)} days";
            public decimal Total { get; set; }
            public string TotalDisplay => $"₹{Total:N1}";
        }

        private List<PendingChallanRow> _allChallans = new();

        public PendingChallansControl()
        {
            InitializeComponent();
            NumericInputHelper.AttachDigitsOnly(MinDaysPendingBox);
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var vendors = await VendorRepository.GetVendorsAsync();
                var vendorList = new List<string> { "Any vendor" };
                vendorList.AddRange(vendors.Select(v => v.Name).OrderBy(n => n));
                VendorFilterCombo.ItemsSource = vendorList;
                VendorFilterCombo.SelectedIndex = 0;

                var purchases = await PurchaseRepository.GetPurchasesAsync(null, null, null, null);

                var pendingPurchases = purchases.Where(p =>
                    !string.IsNullOrWhiteSpace(p.ChallanNumber) &&
                    (p.InvoiceStatus == "Pending" || string.IsNullOrWhiteSpace(p.InvoiceNumber))
                ).ToList();

                _allChallans = pendingPurchases.Select(p => new PendingChallanRow
                {
                    Id = p.Id,
                    ChallanNumber = string.IsNullOrWhiteSpace(p.ChallanNumber) ? "—" : p.ChallanNumber,
                    VendorName = p.VendorName,
                    Date = p.ChallanDate ?? p.PurchaseDate,
                    Total = p.Total
                }).OrderByDescending(c => c.DaysPending).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load pending challans report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            VendorFilterCombo.SelectedIndex = 0;
            MinDaysPendingBox.Text = "";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allChallans == null) return;

            string? vendor = VendorFilterCombo.SelectedItem as string;
            if (vendor == "Any vendor") vendor = null;

            int? minDays = int.TryParse(MinDaysPendingBox.Text.Trim(), out var days) ? days : null;

            var filtered = _allChallans.Where(c =>
            {
                if (!string.IsNullOrEmpty(vendor) && !c.VendorName.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (minDays.HasValue && c.DaysPending < minDays.Value)
                    return false;

                return true;
            }).ToList();

            ChallanGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalAmt = filtered.Sum(c => c.Total);
            SummaryBadgeText.Text = $"{filtered.Count} pending challans · Total: ₹{totalAmt:N1}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = ChallanGrid.ItemsSource as List<PendingChallanRow>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(c => new List<string>
            {
                c.ChallanNumber,
                c.VendorName,
                c.DateDisplay,
                c.DaysPendingDisplay,
                c.TotalDisplay
            }).ToList();

            string fileName = $"pending-challans-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Pending Challans Report",
                new List<string> { "Challan Number", "Vendor", "Challan Date", "Days Pending", "Amount" }, rows);
        }
    }
}
