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
    public partial class IssueGoodsReportControl : UserControl
    {
        public class IssueGoodsReportRow
        {
            public string EventName { get; set; } = "";
            public string CategoryDisplay { get; set; } = "—";
            public string ItemName { get; set; } = "";
            public decimal Qty { get; set; }
            public string QtyDisplay => Qty.ToString("N0");
            public string Status { get; set; } = "Pending";
            public Brush StatusBgBrush => Status == "Deposited"
                ? new SolidColorBrush(Color.FromRgb(0xEB, 0xFB, 0xEE))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0xF5, 0xEC));
            public Brush StatusFgBrush => Status == "Deposited"
                ? new SolidColorBrush(Color.FromRgb(0x2B, 0x8A, 0x3E))
                : new SolidColorBrush(Color.FromRgb(0xE2, 0x60, 0x0A));

            public string ReceiverName { get; set; } = "";
            public DateTime DepositDate { get; set; }
            public string DepositDateDisplay => DepositDate.ToString("dd MMM yyyy");
        }

        private List<IssueGoodsReportRow> _allRows = new();

        public IssueGoodsReportControl()
        {
            InitializeComponent();
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var categories = await ProductRepository.GetCategoriesAsync();
                var catList = new List<string> { "Any category" };
                catList.AddRange(categories.Select(c => c.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n));
                CategoryFilterCombo.ItemsSource = catList;
                CategoryFilterCombo.SelectedIndex = 0;

                var products = await ProductRepository.GetProductsAsync();
                var prodList = new List<string> { "Any item" };
                prodList.AddRange(products.Select(p => p.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n));
                ItemFilterCombo.ItemsSource = prodList;
                ItemFilterCombo.SelectedIndex = 0;

                var prodCategoryMap = products
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Category ?? "—", StringComparer.OrdinalIgnoreCase);

                var records = await IssueRepository.GetRecordsAsync(null, null, null);

                _allRows = records.Select(r =>
                {
                    string cat = prodCategoryMap.TryGetValue(r.Item, out var foundCat) ? foundCat : "—";
                    return new IssueGoodsReportRow
                    {
                        EventName = r.EventName,
                        CategoryDisplay = string.IsNullOrWhiteSpace(cat) ? "—" : cat,
                        ItemName = r.Item,
                        Qty = r.Qty,
                        Status = r.Status,
                        ReceiverName = r.ReceiverName,
                        DepositDate = r.DepositDate
                    };
                }).OrderByDescending(r => r.DepositDate).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load issue goods report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e) => ApplyFilters();

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            CategoryFilterCombo.SelectedIndex = 0;
            ItemFilterCombo.SelectedIndex = 0;
            StatusFilterCombo.SelectedIndex = 0;
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allRows == null) return;

            string? selectedCat = CategoryFilterCombo.SelectedItem as string;
            if (selectedCat == "Any category") selectedCat = null;

            string? selectedItem = ItemFilterCombo.SelectedItem as string;
            if (selectedItem == "Any item") selectedItem = null;

            string? selectedStatus = (StatusFilterCombo.SelectedItem as ComboBoxItem)?.Content as string;
            if (selectedStatus == "Any status") selectedStatus = null;

            DateTime? fromDate = FromDatePicker.SelectedDate?.Date;
            DateTime? toDate = ToDatePicker.SelectedDate?.Date;

            var filtered = _allRows.Where(r =>
            {
                if (!string.IsNullOrEmpty(selectedCat) && !r.CategoryDisplay.Equals(selectedCat, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedItem) && !r.ItemName.Equals(selectedItem, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(selectedStatus) && !r.Status.Equals(selectedStatus, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (fromDate.HasValue && r.DepositDate.Date < fromDate.Value)
                    return false;

                if (toDate.HasValue && r.DepositDate.Date > toDate.Value)
                    return false;

                return true;}).ToList();

            IssueGrid.ItemsSource = filtered;
            EmptyGridText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            decimal totalQty = filtered.Sum(r => r.Qty);
            SummaryBadgeText.Text = $"{filtered.Count} lines · Total Qty Issued: {totalQty:N0}";
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filtered = IssueGrid.ItemsSource as List<IssueGoodsReportRow>;
            if (filtered == null || filtered.Count == 0)
            {
                MessageBox.Show("There are no records to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var rows = filtered.Select(r => new List<string>
            {
                r.EventName,
                r.CategoryDisplay,
                r.ItemName,
                r.QtyDisplay,
                r.Status,
                r.ReceiverName,
                r.DepositDateDisplay
            }).ToList();

            string fileName = $"issue-goods-report-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Issue Goods Report",
                new List<string> { "Event", "Category", "Item", "Qty", "Status", "Receiver", "Deposit Date" }, rows);
        }
    }
}
