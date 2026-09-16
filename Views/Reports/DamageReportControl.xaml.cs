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
    public partial class DamageReportControl : UserControl
    {
        private List<DamageRecord> _allDamage = new();

        public DamageReportControl()
        {
            InitializeComponent();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var categories = await ProductRepository.GetCategoriesAsync();
                var catList = new List<string> { "Any category" };
                catList.AddRange(categories.Select(c => c.Name).OrderBy(n => n));
                CategoryFilterCombo.ItemsSource = catList;
                CategoryFilterCombo.SelectedIndex = 0;

                var products = await ProductRepository.GetProductsAsync();
                var prodList = new List<string> { "Any item" };
                prodList.AddRange(products.Select(p => p.Name).OrderBy(n => n));
                ItemFilterCombo.ItemsSource = prodList;
                ItemFilterCombo.SelectedIndex = 0;

                _allDamage = await DamageRepository.GetRecordsAsync(null, null);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load damage report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            CategoryFilterCombo.SelectedIndex = 0;
            ItemFilterCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allDamage == null) return;

            string? selectedCat = CategoryFilterCombo.SelectedItem as string;
            if (selectedCat == "Any category") selectedCat = null;

            string? selectedItem = ItemFilterCombo.SelectedItem as string;
            if (selectedItem == "Any item") selectedItem = null;

            DateTime? fromDate = FromDatePicker.SelectedDate?.Date;
            DateTime? toDate = ToDatePicker.SelectedDate?.Date;

            var filtered = _allDamage.Where(d =>
            {
                if (!string.IsNullOrEmpty(selectedCat) && !string.Equals(d.Category, selectedCat, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedItem) && !string.Equals(d.ItemName, selectedItem, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (fromDate.HasValue && d.DamageDate.Date < fromDate.Value)
                    return false;

                if (toDate.HasValue && d.DamageDate.Date > toDate.Value)
                    return false;

                return true;
            }).OrderByDescending(d => d.DamageDate).ToList();

            DamageGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalQty = filtered.Sum(d => d.Qty);
            SummaryBadgeText.Text = $"{filtered.Count} reports · Total Qty Damaged: {totalQty:N0}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = DamageGrid.ItemsSource as List<DamageRecord>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(d => new List<string>
            {
                d.DamageCode,
                d.CategoryDisplay,
                d.ItemName,
                d.QtyDisplay,
                d.DateDisplay,
                d.ReporteeName ?? "—",
                d.RemarkDisplay
            }).ToList();

            string fileName = $"damage-report-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Damage Report",
                new List<string> { "Code", "Category", "Item", "Qty", "Date", "Reported By", "Remark" }, rows);
        }
    }
}
