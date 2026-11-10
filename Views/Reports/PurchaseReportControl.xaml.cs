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
    public partial class PurchaseReportControl : UserControl
    {
        private bool _isItemWiseMode = false;
        private List<Purchase> _allPurchases = new();
        private List<PurchaseItemReportRow> _allPurchaseItems = new();

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

                // 2. Load Items / Products
                var products = await ProductRepository.GetProductsAsync();
                var itemList = new List<string> { "Any item" };
                itemList.AddRange(products.Select(p => p.Name).Distinct().OrderBy(n => n));
                ItemFilterCombo.ItemsSource = itemList;
                ItemFilterCombo.SelectedIndex = 0;

                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load purchase report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnPurchaseWise_Click(object sender, RoutedEventArgs e)
        {
            if (!_isItemWiseMode) return;
            _isItemWiseMode = false;
            UpdateToggleUI();
            await ApplyFiltersAsync();
        }

        private async void BtnItemWise_Click(object sender, RoutedEventArgs e)
        {
            if (_isItemWiseMode) return;
            _isItemWiseMode = true;
            UpdateToggleUI();
            await ApplyFiltersAsync();
        }

        private void UpdateToggleUI()
        {
            var saffronBrush = (Brush)FindResource("SaffronDeepBrush");
            var inkSoftBrush = (Brush)FindResource("InkSoftBrush");

            if (!_isItemWiseMode)
            {
                BtnPurchaseWise.Background = saffronBrush;
                BtnPurchaseWise.Foreground = Brushes.White;

                BtnItemWise.Background = Brushes.Transparent;
                BtnItemWise.Foreground = inkSoftBrush;

                PurchaseGrid.Visibility = Visibility.Visible;
                ItemPurchaseGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnItemWise.Background = saffronBrush;
                BtnItemWise.Foreground = Brushes.White;

                BtnPurchaseWise.Background = Brushes.Transparent;
                BtnPurchaseWise.Foreground = inkSoftBrush;

                PurchaseGrid.Visibility = Visibility.Collapsed;
                ItemPurchaseGrid.Visibility = Visibility.Visible;
            }
        }

        private async void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => await ApplyFiltersAsync();

        private async void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            VendorFilterCombo.SelectedIndex = 0;
            ItemFilterCombo.SelectedIndex = 0;
            ReceivedAgainstCombo.SelectedIndex = 0;
            PaymentModeCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            MinAmountBox.Text = "";
            MaxAmountBox.Text = "";
            await ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                string? selectedProduct = ItemFilterCombo.SelectedItem as string;
                if (selectedProduct == "Any item") selectedProduct = null;

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

                if (!_isItemWiseMode)
                {
                    _allPurchases = await PurchaseRepository.GetPurchasesAsync(null, selectedProduct, null, null);
                    if (_allPurchases == null) return;

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
                else
                {
                    _allPurchaseItems = await PurchaseRepository.GetPurchaseItemsReportAsync(null, selectedProduct, null, null);
                    if (_allPurchaseItems == null) return;

                    var filtered = _allPurchaseItems.Where(item =>
                    {
                        if (!string.IsNullOrEmpty(selectedVendor) && !string.Equals(item.VendorName, selectedVendor, StringComparison.OrdinalIgnoreCase))
                            return false;

                        if (!string.IsNullOrEmpty(selectedReceivedAgainst))
                        {
                            if (selectedReceivedAgainst.Equals("PO", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(item.PoReference))
                                return false;
                            if (selectedReceivedAgainst.Equals("Direct", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.PoReference))
                                return false;
                        }

                        if (!string.IsNullOrEmpty(selectedPayment) && !string.Equals(item.PaymentMode, selectedPayment, StringComparison.OrdinalIgnoreCase))
                            return false;

                        if (fromDate.HasValue && item.PurchaseDate.Date < fromDate.Value)
                            return false;

                        if (toDate.HasValue && item.PurchaseDate.Date > toDate.Value)
                            return false;

                        if (minAmt.HasValue && item.TotalAmount < minAmt.Value)
                            return false;

                        if (maxAmt.HasValue && item.TotalAmount > maxAmt.Value)
                            return false;

                        return true;
                    }).OrderByDescending(item => item.PurchaseDate).ThenByDescending(item => item.PurchaseId).ToList();

                    ItemPurchaseGrid.ItemsSource = filtered;
                    EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                    decimal totalAmount = filtered.Sum(item => item.TotalAmount);
                    SummaryBadgeText.Text = $"{filtered.Count} items · Total: ₹{totalAmount:N2}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not filter purchases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isItemWiseMode)
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
                    p.SerialNumbersDisplay,
                    p.PurchaseType,
                    p.PaymentModeDisplay,
                    p.TotalDisplay
                }).ToList();

                string fileName = $"purchase-report-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
                ExcelExportHelper.Export(fileName, "Purchase Report",
                    new List<string> { "Date", "Vendor", "PO Ref", "Invoice / Challan", "Serial No", "Purchase Type", "Payment Mode", "Total Amount" }, rows);
            }
            else
            {
                var filtered = ItemPurchaseGrid.ItemsSource as List<PurchaseItemReportRow>;
                if (filtered == null || filtered.Count == 0)
                {
                    MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Item Wise columns: DATE, CATEGORY, PRODUCT / ITEM, SERIAL NO, QTY, UNIT PRICE, GST %, TAX AMT, TOTAL
                var rows = filtered.Select(item => new List<string>
                {
                    item.PurchaseDateDisplay,
                    item.CategoryDisplay,
                    item.ProductName,
                    item.SerialNumberDisplay,
                    item.QtyDisplay,
                    item.PriceDisplay,
                    item.GstPercentDisplay,
                    item.GstAmountDisplay,
                    item.TotalAmountDisplay
                }).ToList();

                string fileName = $"purchase-item-report-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
                ExcelExportHelper.Export(fileName, "Purchase Items Report",
                    new List<string> { "Date", "Category", "Product / Item", "Serial No", "Qty", "Unit Price", "GST %", "Tax Amount", "Total Amount" }, rows);
            }
        }
    }
}
