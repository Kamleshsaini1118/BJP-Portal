using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp.Views.Purchases
{
    public partial class GoodsReturnControl : UserControl
    {
        private List<GoodsReturn> _allReturns = new();
        private List<string> _vendorNames = new();
        private List<string> _categoryNames = new();
        private List<string> _productNames = new();
        private bool _suppressFilters;

        public event Func<Task>? ReturnSaved;

        public GoodsReturnControl()
        {
            InitializeComponent();
        }

        public async Task LoadDataAsync(
            List<Vendor>? vendors = null,
            List<ProductCategory>? categories = null,
            List<Product>? products = null)
        {
            try
            {
                if (vendors != null)
                {
                    _vendorNames = vendors
                        .Select(v => v.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n)
                        .ToList();
                }

                if (categories != null)
                {
                    _categoryNames = categories
                        .Where(c => c.IsActive)
                        .Select(c => c.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n)
                        .ToList();
                }

                if (products != null)
                {
                    _productNames = products
                        .Select(p => p.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n)
                        .ToList();
                }

                PopulateFilterCombos();
                _allReturns = await GoodsReturnRepository.GetGoodsReturnsAsync();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load goods returns.\n\n{ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateFilterCombos()
        {
            _suppressFilters = true;
            try
            {
                string? prevVendor = VendorFilterCombo.SelectedItem as string;
                var vendorOptions = new List<string> { "Any vendor" };
                vendorOptions.AddRange(_vendorNames);
                VendorFilterCombo.ItemsSource = vendorOptions;
                VendorFilterCombo.SelectedItem = prevVendor != null && vendorOptions.Contains(prevVendor) ? prevVendor : "Any vendor";

                string? prevCategory = CategoryFilterCombo.SelectedItem as string;
                var categoryOptions = new List<string> { "Any category" };
                categoryOptions.AddRange(_categoryNames);
                CategoryFilterCombo.ItemsSource = categoryOptions;
                CategoryFilterCombo.SelectedItem = prevCategory != null && categoryOptions.Contains(prevCategory) ? prevCategory : "Any category";

                string? prevProduct = ProductFilterCombo.SelectedItem as string;
                var productOptions = new List<string> { "Any product" };
                productOptions.AddRange(_productNames);
                ProductFilterCombo.ItemsSource = productOptions;
                ProductFilterCombo.SelectedItem = prevProduct != null && productOptions.Contains(prevProduct) ? prevProduct : "Any product";

                string? prevReason = ReasonFilterCombo.SelectedItem as string;
                var reasonOptions = new List<string> { "Any reason" };
                reasonOptions.AddRange(AddGoodsReturnWindow.DefaultReasons);
                ReasonFilterCombo.ItemsSource = reasonOptions;
                ReasonFilterCombo.SelectedItem = prevReason != null && reasonOptions.Contains(prevReason) ? prevReason : "Any reason";
            }
            finally
            {
                _suppressFilters = false;
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_suppressFilters) ApplyFilters();
        }

        private void DateFilterChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_suppressFilters) ApplyFilters();
        }

        private void ReturnSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_suppressFilters) ApplyFilters();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            _suppressFilters = true;
            try
            {
                ReturnSearchBox.Text = "";
                VendorFilterCombo.SelectedIndex = 0;
                CategoryFilterCombo.SelectedIndex = 0;
                ProductFilterCombo.SelectedIndex = 0;
                ReasonFilterCombo.SelectedIndex = 0;
                FromDatePicker.SelectedDate = null;
                ToDatePicker.SelectedDate = null;
            }
            finally
            {
                _suppressFilters = false;
            }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allReturns == null) return;

            string query = ReturnSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            string? selectedVendor = VendorFilterCombo.SelectedItem as string;
            if (selectedVendor == "Any vendor") selectedVendor = null;

            string? selectedCategory = CategoryFilterCombo.SelectedItem as string;
            if (selectedCategory == "Any category") selectedCategory = null;

            string? selectedProduct = ProductFilterCombo.SelectedItem as string;
            if (selectedProduct == "Any product") selectedProduct = null;

            string? selectedReason = ReasonFilterCombo.SelectedItem as string;
            if (selectedReason == "Any reason") selectedReason = null;

            DateTime? fromDate = FromDatePicker.SelectedDate?.Date;
            DateTime? toDate = ToDatePicker.SelectedDate?.Date;

            var filtered = _allReturns.Where(r =>
            {
                if (!string.IsNullOrEmpty(selectedVendor) &&
                    !r.VendorName.Equals(selectedVendor, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedCategory) &&
                    !ContainsToken(r.CategoriesList, selectedCategory))
                    return false;

                if (!string.IsNullOrEmpty(selectedProduct) &&
                    !ContainsToken(r.ProductsList, selectedProduct))
                    return false;

                if (!string.IsNullOrEmpty(selectedReason) &&
                    !r.Reason.Equals(selectedReason, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (fromDate.HasValue && r.ReturnDate.Date < fromDate.Value)
                    return false;

                if (toDate.HasValue && r.ReturnDate.Date > toDate.Value)
                    return false;

                if (!string.IsNullOrEmpty(query))
                {
                    bool matches = r.ReturnNo.ToLowerInvariant().Contains(query)
                        || r.VendorName.ToLowerInvariant().Contains(query)
                        || r.AgainstPurchaseRef.ToLowerInvariant().Contains(query)
                        || r.ItemsSummary.ToLowerInvariant().Contains(query)
                        || r.Reason.ToLowerInvariant().Contains(query);
                    if (!matches) return false;
                }

                return true;
            }).ToList();

            ReturnsGrid.ItemsSource = filtered;
            EmptyStateText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalQty = filtered.Sum(r => r.TotalQty);
            decimal totalAmt = filtered.Sum(r => r.TotalAmount);
            string qtyStr = totalQty % 1 == 0 ? totalQty.ToString("N0") : totalQty.ToString("0.##");
            string amtStr = totalAmt % 1 == 0 ? $"₹{totalAmt:N0}" : $"₹{totalAmt:N2}";
            string noun = filtered.Count == 1 ? "return" : "returns";
            SummaryBadgeText.Text = $"{filtered.Count} {noun} · Qty: {qtyStr} · Amount: {amtStr}";
        }

        private static bool ContainsToken(string commaSeparated, string target)
        {
            if (string.IsNullOrWhiteSpace(commaSeparated)) return false;
            return commaSeparated
                .Split(',')
                .Select(s => s.Trim())
                .Any(s => s.Equals(target, StringComparison.OrdinalIgnoreCase));
        }

        private async void AddReturnButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vendorNames.Count == 0)
                {
                    var vendors = await VendorRepository.GetVendorsAsync();
                    _vendorNames = vendors.Select(v => v.Name).Where(n => !string.IsNullOrWhiteSpace(n)).OrderBy(n => n).ToList();
                }

                var ownerWindow = Window.GetWindow(this);
                var dialog = new AddGoodsReturnWindow(_vendorNames) { Owner = ownerWindow };
                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync();
                    if (ReturnSaved != null)
                    {
                        await ReturnSaved.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open Return to Vendor dialog.\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReturnNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: GoodsReturn selected }) return;

            try
            {
                var (header, items) = await GoodsReturnRepository.GetGoodsReturnDetailsAsync(selected.Id);
                if (header == null)
                {
                    MessageBox.Show("Could not find details for this return record.", "Not Found",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var ownerWindow = Window.GetWindow(this);
                new GoodsReturnNoteWindow(header, items) { Owner = ownerWindow }.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load Return Note.\n\n{ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = ReturnsGrid.ItemsSource as List<GoodsReturn>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no returns to export.", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(r => new List<string>
            {
                r.ReturnNo,
                r.ReturnDateDisplay,
                r.VendorName,
                $"{r.AgainstPurchaseRef} ({r.PurchaseDateDisplay})",
                r.ItemsDisplay,
                r.Reason,
                r.TotalQtyDisplay,
                r.TotalAmountDisplay,
                r.HandedOverToDisplay,
                r.ContactNumberDisplay,
                r.RemarksDisplay
            }).ToList();

            string fileName = $"goods-returns-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(
                fileName,
                "Goods Returns",
                new List<string>
                {
                    "Return No.", "Return Date", "Vendor", "Against Purchase",
                    "Items", "Reason", "Total Qty", "Amount",
                    "Handed Over To", "Contact Number", "Remarks"
                },
                rows);
        }
    }
}
