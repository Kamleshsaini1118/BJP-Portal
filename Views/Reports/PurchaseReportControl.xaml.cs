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
    public partial class PurchaseReportControl : UserControl
    {
        private List<Purchase> _allPurchases = new();

        public PurchaseReportControl()
        {
            InitializeComponent();
            NumericInputHelper.AttachDecimalOnly(MinAmountBox);
            NumericInputHelper.AttachDecimalOnly(MaxAmountBox);
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

                _allPurchases = await PurchaseRepository.GetPurchasesAsync(null, null, null, null);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load purchase report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            VendorFilterCombo.SelectedIndex = 0;
            ReceivedAgainstCombo.SelectedIndex = 0;
            PaymentModeCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            MinAmountBox.Text = "";
            MaxAmountBox.Text = "";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allPurchases == null) return;

            string? selectedVendor = VendorFilterCombo.SelectedItem as string;
            if (selectedVendor == "Any vendor") selectedVendor = null;

            string? selectedReceivedAgainst = (ReceivedAgainstCombo.SelectedItem as ComboBoxItem)?.Content as string;
            if (selectedReceivedAgainst == "Any") selectedReceivedAgainst = null;

            string? selectedPayment = (PaymentModeCombo.SelectedItem as ComboBoxItem)?.Content as string;
            if (selectedPayment == "Any mode" || selectedPayment == "Any") selectedPayment = null;

            DateTime? fromDate = FromDatePicker.SelectedDate?.Date;
            DateTime? toDate = ToDatePicker.SelectedDate?.Date;

            decimal? minAmt = decimal.TryParse(MinAmountBox.Text.Trim(), out var minVal) ? minVal : null;
            decimal? maxAmt = decimal.TryParse(MaxAmountBox.Text.Trim(), out var maxVal) ? maxVal : null;

            var filtered = _allPurchases.Where(p =>
            {
                if (!string.IsNullOrEmpty(selectedVendor) && !string.Equals(p.VendorName, selectedVendor, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedReceivedAgainst))
                {
                    if (selectedReceivedAgainst.Equals("PO", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(p.PoReference))
                        return false;
                    if (selectedReceivedAgainst.Equals("Direct", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(p.PoReference))
                        return false;
                }

                if (!string.IsNullOrEmpty(selectedPayment) && !string.Equals(p.PaymentMode, selectedPayment, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (fromDate.HasValue && p.PurchaseDate.Date < fromDate.Value)
                    return false;

                if (toDate.HasValue && p.PurchaseDate.Date > toDate.Value)
                    return false;

                if (minAmt.HasValue && p.Total < minAmt.Value)
                    return false;

                if (maxAmt.HasValue && p.Total > maxAmt.Value)
                    return false;

                return true;
            }).OrderByDescending(p => p.PurchaseDate).ToList();

            PurchaseGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalAmount = filtered.Sum(p => p.Total);
            SummaryBadgeText.Text = $"{filtered.Count} purchases · Total: ₹{totalAmount:N2}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = PurchaseGrid.ItemsSource as List<Purchase>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(p => new List<string>
            {
                p.PurchaseDateDisplay,
                p.VendorName,
                p.PoReferenceDisplay,
                p.InvoiceOrChallanDisplay,
                p.PurchaseType,
                p.PaymentModeDisplay,
                p.TotalDisplay
            }).ToList();

            string fileName = $"purchase-report-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Purchase Report",
                new List<string> { "Date", "Vendor", "PO Ref", "Invoice / Challan", "Purchase Type", "Payment Mode", "Total Amount" }, rows);
        }
    }
}
