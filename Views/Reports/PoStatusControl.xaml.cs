using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp.Views.Reports
{
    public partial class PoStatusControl : UserControl
    {
        public class PoStatusRow
        {
            public int Id { get; set; }
            public string PoReference { get; set; } = "";
            public string VendorName { get; set; } = "";
            public DateTime CreatedDate { get; set; }
            public string CreatedDateDisplay => CreatedDate.ToString("dd MMM yyyy");
            public DateTime? Delivery { get; set; }
            public string DeliveryDisplay => Delivery.HasValue ? Delivery.Value.ToString("dd MMM yyyy") : "—";
            public string StatusText { get; set; } = "Approved";
            public Brush StatusBgBrush => StatusText == "Approved"
                ? new SolidColorBrush(Color.FromRgb(0xEB, 0xFB, 0xEE))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0xEC));
            public Brush StatusFgBrush => StatusText == "Approved"
                ? new SolidColorBrush(Color.FromRgb(0x2B, 0x8A, 0x3E))
                : new SolidColorBrush(Color.FromRgb(0xE2, 0x60, 0x0A));

            public bool IsConverted { get; set; }
            public string ConvertedText => IsConverted ? "Yes" : "No";
            public Brush ConvertedBgBrush => IsConverted
                ? new SolidColorBrush(Color.FromRgb(0xEB, 0xFB, 0xEE))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0xEC));
            public Brush ConvertedFgBrush => IsConverted
                ? new SolidColorBrush(Color.FromRgb(0x2B, 0x8A, 0x3E))
                : new SolidColorBrush(Color.FromRgb(0xE2, 0x60, 0x0A));

            public decimal TotalAmount { get; set; }
            public string TotalAmountDisplay => $"₹{TotalAmount:N0}";
        }

        private List<PoStatusRow> _allRows = new();

        public PoStatusControl()
        {
            InitializeComponent();
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

                var pos = await PurchaseOrderRepository.GetOrdersAsync(null, null, null, null);
                var purchases = await PurchaseRepository.GetPurchasesAsync(null, null, null, null);
                var purchasePoRefs = purchases
                    .Where(p => !string.IsNullOrWhiteSpace(p.PoReference))
                    .Select(p => p.PoReference!.Trim().ToLowerInvariant())
                    .ToHashSet();

                _allRows = new List<PoStatusRow>();
                foreach (var po in pos)
                {
                    bool isConverted = !string.IsNullOrWhiteSpace(po.PoReference) && purchasePoRefs.Contains(po.PoReference.Trim().ToLowerInvariant());

                    decimal totalAmount = 0;
                    try
                    {
                        var poItems = await PurchaseOrderRepository.GetItemsAsync(po.Id);
                        totalAmount = poItems.Sum(i =>
                        {
                            _ = decimal.TryParse(i.Qty, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var q);
                            _ = decimal.TryParse(i.Price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p);
                            return q * p * (1 + (i.GstPercent / 100m));
                        });
                    }
                    catch { }

                    _allRows.Add(new PoStatusRow
                    {
                        Id = po.Id,
                        PoReference = po.PoReference,
                        VendorName = po.VendorName,
                        CreatedDate = po.CreatedDate,
                        Delivery = po.Delivery,
                        StatusText = "Approved",
                        IsConverted = isConverted,
                        TotalAmount = totalAmount
                    });
                }

                _allRows = _allRows.OrderByDescending(po => po.CreatedDate).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load PO status report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            VendorFilterCombo.SelectedIndex = 0;
            StatusFilterCombo.SelectedIndex = 0;
            ConvertedFilterCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allRows == null) return;

            string? vendor = VendorFilterCombo.SelectedItem as string;
            if (vendor == "Any vendor") vendor = null;

            string? status = (StatusFilterCombo.SelectedItem as ComboBoxItem)?.Content as string;
            if (status == "Any status") status = null;

            string? converted = (ConvertedFilterCombo.SelectedItem as ComboBoxItem)?.Content as string;
            if (converted == "Any") converted = null;

            DateTime? fromDate = FromDatePicker.SelectedDate?.Date;
            DateTime? toDate = ToDatePicker.SelectedDate?.Date;

            var filtered = _allRows.Where(po =>
            {
                if (!string.IsNullOrEmpty(vendor) && !po.VendorName.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(status) && !po.StatusText.Equals(status, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(converted))
                {
                    if (converted == "Yes" && !po.IsConverted) return false;
                    if (converted == "No" && po.IsConverted) return false;
                }

                if (fromDate.HasValue && po.CreatedDate.Date < fromDate.Value)
                    return false;

                if (toDate.HasValue && po.CreatedDate.Date > toDate.Value)
                    return false;

                return true;
            }).ToList();

            PoGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalVal = filtered.Sum(po => po.TotalAmount);
            SummaryBadgeText.Text = $"{filtered.Count} POs · Total Value: ₹{totalVal:N0}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = PoGrid.ItemsSource as List<PoStatusRow>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(po => new List<string>
            {
                po.PoReference,
                po.VendorName,
                po.CreatedDateDisplay,
                po.DeliveryDisplay,
                po.StatusText,
                po.ConvertedText,
                po.TotalAmountDisplay
            }).ToList();

            string fileName = $"po-status-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "PO Status Report",
                new List<string> { "PO Reference", "Vendor", "Created Date", "Delivery Date", "Status", "Converted", "Value" }, rows);
        }
    }
}
