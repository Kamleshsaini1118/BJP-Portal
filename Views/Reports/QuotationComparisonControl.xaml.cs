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
    public partial class QuotationComparisonControl : UserControl
    {
        public class QuotationRow
        {
            public string ProjectTitle { get; set; } = "";
            public string ProjectCategory { get; set; } = "";
            public string VendorName { get; set; } = "";
            public string QuotationNumber { get; set; } = "";
            public decimal Amount { get; set; }
            public string AmountDisplay => $"₹{Amount:N0}";
            public string QuotationStatus { get; set; } = "Pending";
            public bool IsL1 { get; set; }
            public bool IsNotL1 => !IsL1;
            public string ProjectStatus { get; set; } = "Open";
        }

        private List<QuotationRow> _allRows = new();

        public QuotationComparisonControl()
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

                var projects = await QuotationRepository.GetProjectsAsync();

                _allRows = new List<QuotationRow>();
                foreach (var p in projects)
                {
                    var quotes = await QuotationRepository.GetQuotationsForProjectAsync(p.Id);
                    decimal minVal = quotes.Count > 0 ? quotes.Min(q => q.QuotationAmount) : 0;

                    foreach (var q in quotes)
                    {
                        _allRows.Add(new QuotationRow
                        {
                            ProjectTitle = p.ProjectName,
                            ProjectCategory = p.Category,
                            VendorName = q.VendorName,
                            QuotationNumber = string.IsNullOrWhiteSpace(q.QuotationNumber) ? $"QT-{q.Id}" : q.QuotationNumber,
                            Amount = q.QuotationAmount,
                            QuotationStatus = q.Status,
                            IsL1 = (q.QuotationAmount == minVal && minVal > 0),
                            ProjectStatus = p.Status
                        });
                    }
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load quotation comparison report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            CategoryFilterCombo.SelectedIndex = 0;
            StatusFilterCombo.SelectedIndex = 0;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allRows == null) return;

            string? cat = CategoryFilterCombo.SelectedItem as string;
            if (cat == "Any category") cat = null;

            string? status = (StatusFilterCombo.SelectedItem as ComboBoxItem)?.Content as string;
            if (status == "Any status") status = null;

            var filtered = _allRows.Where(r =>
            {
                if (!string.IsNullOrEmpty(cat) && !r.ProjectCategory.Equals(cat, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(status) && !r.ProjectStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            }).ToList();

            QuotationGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int l1Count = filtered.Count(r => r.IsL1);
            SummaryBadgeText.Text = $"{filtered.Count} quotations across {l1Count} L1 picks";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = QuotationGrid.ItemsSource as List<QuotationRow>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(r => new List<string>
            {
                r.ProjectTitle,
                r.VendorName,
                r.QuotationNumber,
                r.AmountDisplay,
                r.QuotationStatus,
                r.IsL1 ? "L1" : "—",
                r.ProjectStatus
            }).ToList();

            string fileName = $"quotation-comparison-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Quotation Comparison Report",
                new List<string> { "Project", "Vendor", "Quotation Number", "Amount", "Quotation Status", "L1 (Lowest)", "Project Status" }, rows);
        }
    }
}
