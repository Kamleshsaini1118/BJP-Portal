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
    public partial class DistributionReportControl : UserControl
    {
        private List<DistributionSummary> _allDistributions = new();

        public DistributionReportControl()
        {
            InitializeComponent();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var offices = await OfficeRepository.GetOfficesAsync();
                var officeNames = new List<string> { "Any destination" };
                officeNames.AddRange(offices.Select(o => o.Name).OrderBy(n => n));
                DestinationFilterCombo.ItemsSource = officeNames;
                DestinationFilterCombo.SelectedIndex = 0;

                var products = await ProductRepository.GetProductsAsync();
                var productNames = new List<string> { "Any item" };
                productNames.AddRange(products.Select(p => p.Name).OrderBy(n => n));
                ItemFilterCombo.ItemsSource = productNames;
                ItemFilterCombo.SelectedIndex = 0;

                _allDistributions = await DistributionRepository.GetDistributionsAsync();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load distribution report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            DestinationFilterCombo.SelectedIndex = 0;
            ItemFilterCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allDistributions == null) return;

            string? dest = DestinationFilterCombo.SelectedItem as string;
            if (dest == "Any destination") dest = null;

            string? item = ItemFilterCombo.SelectedItem as string;
            if (item == "Any item") item = null;

            DateTime? fromDate = FromDatePicker.SelectedDate?.Date;
            DateTime? toDate = ToDatePicker.SelectedDate?.Date;

            var filtered = _allDistributions.Where(d =>
            {
                if (!string.IsNullOrEmpty(dest) && !d.Destination.Equals(dest, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(item) && !d.ItemsList.ToLowerInvariant().Contains(item.ToLowerInvariant()))
                    return false;

                if (fromDate.HasValue && d.DistributionDate.Date < fromDate.Value)
                    return false;

                if (toDate.HasValue && d.DistributionDate.Date > toDate.Value)
                    return false;

                return true;
            }).OrderByDescending(d => d.DistributionDate).ToList();

            DistributionGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalQty = filtered.Sum(d => d.TotalQty);
            SummaryBadgeText.Text = $"{filtered.Count} lines · Total Qty: {totalQty:N0}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = DistributionGrid.ItemsSource as List<DistributionSummary>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(d => new List<string>
            {
                d.DateDisplay,
                d.Destination,
                d.ItemsList,
                d.TotalQtyDisplay,
                d.ShipmentId
            }).ToList();

            string fileName = $"distribution-report-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Distribution Report",
                new List<string> { "Date", "Destination", "Item", "Qty", "Source (PO/Invoice)" }, rows);
        }
    }
}
