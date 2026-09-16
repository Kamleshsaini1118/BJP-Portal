using System;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace StockPortalApp
{
    public partial class DashboardWindow : Window
    {
        private readonly Window _loginWindow;
        private List<Vendor> _allVendors = new();
        private List<Product> _allProducts = new();
        private List<ProductCategory> _categories = new();

        private static readonly Dictionary<string, string> TitleMap = new()
        {
            ["dashboard"] = "Dashboard Overview",
            ["food-packets"] = "Food Packets",
            ["fixed-assets"] = "Fixed Assets",
            ["stock-report"] = "Stock Report",
            ["damage"] = "Damage",
            ["issue-borrow"] = "Issue / Borrow",
            ["raise-po"] = "Raise PO",
            ["add-purchase"] = "Add Purchase",
            ["add-export"] = "Add Distribution",
            ["add-vendor"] = "Add Vendor",
            ["add-product"] = "Add Product",
            ["quotation"] = "Quotations",
            ["utility-reminder"] = "Utility Reminders",
            ["reports"] = "Reports",
        };

        public DashboardWindow(Window loginWindow, string displayName)
        {
            InitializeComponent();
            _loginWindow = loginWindow;
            SetUser(displayName);
            Loaded += DashboardWindow_Loaded;

        }

        private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DashDateFrom != null) DashDateFrom.SelectedDate = IndiaTime.Today;
                if (DashDateTo != null) DashDateTo.SelectedDate = null;

                await LoadVendorsAsync();
                await LoadProductsAndCategoriesAsync();
                await LoadBatchesAsync();
                await LoadFixedAssetsAsync();
                await LoadStockReportAsync();
                await LoadDamageAsync();
                IssueReportTypeCombo.ItemsSource = new[] { "All", "Pending", "Deposited" };
                IssueReportTypeCombo.SelectedIndex = 0;
                await LoadIssueRecordsAsync();
                await LoadPurchaseOrdersAsync();
                await LoadPurchasesAsync();
                await LoadDistributionsAsync();
                await LoadProjectsAsync();
                await LoadUtilityRemindersAsync();

                BuildDashboardOverview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data.\n\n{ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // ================= DASHBOARD OVERVIEW =================

        private void DashDateChanged(object sender, SelectionChangedEventArgs e)
        {
            BuildDashboardOverview();
        }

        private void ClearDashFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            if (DashDateFrom != null) DashDateFrom.SelectedDate = IndiaTime.Today;
            if (DashDateTo != null) DashDateTo.SelectedDate = null;
            BuildDashboardOverview();
        }

        /// <summary>
        /// Builds the landing Dashboard page entirely from the data already loaded by the
        /// other panels above -- no extra database round-trips needed.
        /// </summary>
        private void BuildDashboardOverview()
        {
            DateTime? dateFrom = DashDateFrom?.SelectedDate?.Date;
            DateTime? dateTo = DashDateTo?.SelectedDate?.Date;

            var purchases = _allPurchases.Where(p =>
                (!dateFrom.HasValue || p.PurchaseDate.Date >= dateFrom.Value) &&
                (!dateTo.HasValue || p.PurchaseDate.Date <= dateTo.Value)).ToList();

            var pos = _allPurchaseOrders.Where(p =>
                (!dateFrom.HasValue || p.CreatedDate.Date >= dateFrom.Value) &&
                (!dateTo.HasValue || p.CreatedDate.Date <= dateTo.Value)).ToList();

            var damages = _allDamage.Where(d =>
                (!dateFrom.HasValue || d.DamageDate.Date >= dateFrom.Value) &&
                (!dateTo.HasValue || d.DamageDate.Date <= dateTo.Value)).ToList();

            var issues = _allIssueRecords.Where(i =>
            {
                var dt = DateTime.TryParse(i.CreatedAt, out var parsed) ? parsed : i.DepositDate;
                return (!dateFrom.HasValue || dt.Date >= dateFrom.Value) &&
                       (!dateTo.HasValue || dt.Date <= dateTo.Value);
            }).ToList();

            var distributions = _allDistributions.Where(d =>
                (!dateFrom.HasValue || d.DistributionDate.Date >= dateFrom.Value) &&
                (!dateTo.HasValue || d.DistributionDate.Date <= dateTo.Value)).ToList();

            DashVendorCountText.Text = _allVendors.Count.ToString();
            DashProductCountText.Text = _allProducts.Count.ToString();
            DashPoCountText.Text = pos.Count.ToString();
            DashPurchaseValueText.Text = FormatAssetValue(purchases.Sum(p => p.Total));
            DashDistributionCountText.Text = distributions.Count.ToString();
            DashDamageCountText.Text = damages.Count.ToString();
            DashIssuePendingCountText.Text = issues.Count(i => i.Status == "Pending").ToString();
            DashIssueOverdueCountText.Text = issues.Count(i => i.Status == "Pending" && i.DepositDate < IndiaTime.Today).ToString();
            DashAssetsValueText.Text = FormatAssetValue(_allAssets.Sum(a => a.Value));
            DashStockItemCountText.Text = _allStockReport.Count.ToString();
            DashOutOfStockCountText.Text = _allStockReport.Count(s => s.Remaining <= 0).ToString();
            DashBatchCountText.Text = _allBatches.Count.ToString();

            var lowStock = _allStockReport.Where(s => s.TotalQty > 0).OrderBy(s => s.Remaining).Take(5).ToList();
            DashLowStockGrid.ItemsSource = lowStock;
            DashLowStockGrid.Visibility = lowStock.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            DashLowStockEmptyText.Visibility = lowStock.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var pendingIssues = issues.Where(i => i.Status == "Pending").OrderBy(i => i.DepositDate).Take(5).ToList();
            DashPendingIssuesGrid.ItemsSource = pendingIssues;
            DashPendingIssuesGrid.Visibility = pendingIssues.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            DashPendingIssuesEmptyText.Visibility = pendingIssues.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var recentPos = pos.Take(5).ToList();
            DashRecentPoGrid.ItemsSource = recentPos;
            DashRecentPoGrid.Visibility = recentPos.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            DashRecentPoEmptyText.Visibility = recentPos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var recentPurchases = purchases.Take(5).ToList();
            DashRecentPurchaseGrid.ItemsSource = recentPurchases;
            DashRecentPurchaseGrid.Visibility = recentPurchases.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            DashRecentPurchaseEmptyText.Visibility = recentPurchases.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }


        // ================= ISSUE / BORROW =================

        // private List<IssuableCatalogItem> _allCatalogItems = new();
        // private List<ItemIssue> _allIssues = new();

        // private async System.Threading.Tasks.Task LoadIssueBorrowDataAsync()
        // {
        //     try
        //     {
        //         _allCatalogItems = await IssueRepository.GetCatalogAsync();
        //         var reservations = await IssueRepository.GetActiveReservationsAsync();
        //         _allIssues = await IssueRepository.GetIssuesAsync();

        //         ApplyCatalogSearchFilter();
        //         ApplyIssueSearchFilter();

        //         ReservationsGrid.ItemsSource = reservations;
        //         ReservationsEmptyText.Visibility = reservations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        //         ReservationsGrid.Visibility = reservations.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        //         IssueCatalogCountText.Text = _allCatalogItems.Count.ToString();
        //         IssueActiveCountText.Text = _allIssues.Count(i => !i.IsReturned).ToString();
        //         IssueOverdueCountText.Text = _allIssues.Count(i => i.IsOverdue).ToString();
        //     }
        //     catch (System.Exception ex)
        //     {
        //         MessageBox.Show($"Could not load the Issue / Borrow data.\n\n{ex.Message}", "Error",
        //             MessageBoxButton.OK, MessageBoxImage.Error);
        //     }
        // }

        // private void ApplyCatalogSearchFilter()
        // {
        //     var query = CatalogSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
        //     CatalogGrid.ItemsSource = string.IsNullOrEmpty(query)
        //         ? _allCatalogItems
        //         : _allCatalogItems.Where(c =>
        //             c.ProductName.ToLowerInvariant().Contains(query) ||
        //             c.Category.ToLowerInvariant().Contains(query)
        //           ).ToList();
        // }

        // private void CatalogSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyCatalogSearchFilter();

        // private void ApplyIssueSearchFilter()
        // {
        //     var query = IssueSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
        //     IssuesGrid.ItemsSource = string.IsNullOrEmpty(query)
        //         ? _allIssues
        //         : _allIssues.Where(i =>
        //             i.IssueCode.ToLowerInvariant().Contains(query) ||
        //             i.ProductName.ToLowerInvariant().Contains(query) ||
        //             i.BorrowerName.ToLowerInvariant().Contains(query)
        //           ).ToList();
        // }

        // private void IssueSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyIssueSearchFilter();

        // private async void AddCatalogItemButton_Click(object sender, RoutedEventArgs e)
        // {
        //     var dialog = new AddCatalogItemWindow(_allProducts, _allCatalogItems) { Owner = this };
        //     if (dialog.ShowDialog() == true)
        //     {
        //         await LoadIssueBorrowDataAsync();
        //     }
        // }

        // private async void ReserveButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button { DataContext: IssuableCatalogItem item }) return;

        //     var dialog = new ReserveItemWindow(item) { Owner = this };
        //     if (dialog.ShowDialog() == true)
        //     {
        //         await LoadIssueBorrowDataAsync();
        //     }
        // }

        // private async void IssueButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button { DataContext: IssuableCatalogItem item }) return;

        //     var dialog = new IssueItemWindow(item) { Owner = this };
        //     if (dialog.ShowDialog() == true)
        //     {
        //         await LoadIssueBorrowDataAsync();
        //     }
        // }

        // private async void ConvertReservationButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button { DataContext: ItemReservation reservation }) return;

        //     var item = _allCatalogItems.FirstOrDefault(c => c.Id == reservation.IssuableItemId);
        //     if (item == null)
        //     {
        //         MessageBox.Show("Could not find this item in the catalog.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //         return;
        //     }

        //     var dialog = new IssueItemWindow(item, reservation) { Owner = this };
        //     if (dialog.ShowDialog() == true)
        //     {
        //         await LoadIssueBorrowDataAsync();
        //     }
        // }

        // private async void CancelReservationButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button { DataContext: ItemReservation reservation }) return;

        //     var result = MessageBox.Show($"Cancel the reservation for {reservation.BorrowerName}?", "Confirm",
        //         MessageBoxButton.YesNo, MessageBoxImage.Question);
        //     if (result != MessageBoxResult.Yes) return;

        //     await IssueRepository.UpdateReservationStatusAsync(reservation.Id, "Cancelled");
        //     await LoadIssueBorrowDataAsync();
        // }

        // private async void ReturnIssueButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button { DataContext: ItemIssue issue }) return;

        //     var result = MessageBox.Show($"Mark {issue.IssueCode} as returned today?", "Confirm return",
        //         MessageBoxButton.YesNo, MessageBoxImage.Question);
        //     if (result != MessageBoxResult.Yes) return;

        //     await IssueRepository.ReturnIssueAsync(issue.Id, System.DateTime.Today);
        //     await LoadIssueBorrowDataAsync();
        // }

        // private async void RenewIssueButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button { DataContext: ItemIssue issue }) return;

        //     var dialog = new RenewItemWindow(issue) { Owner = this };
        //     if (dialog.ShowDialog() == true)
        //     {
        //         await LoadIssueBorrowDataAsync();
        //     }
        // }


        // ================= ISSUE / BORROW ================= region (from that comment down to right before // ================= ADD DISTRIBUTION =================) with:
        // ================= ISSUE GOODS =================

        private List<IssueRecordLine> _allIssueRecords = new();

        private async System.Threading.Tasks.Task LoadIssueRecordsAsync()
        {
            try
            {
                var status = IssueReportTypeCombo.SelectedItem as string;
                if (status == "All") status = null;

                _allIssueRecords = await IssueRepository.GetRecordsAsync(status, IssueDateFrom.SelectedDate, IssueDateTo.SelectedDate);
                IssueGrid.ItemsSource = _allIssueRecords;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load the issue list.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void IssueFilterChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => await LoadIssueRecordsAsync();

        private async void ClearIssueFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            IssueReportTypeCombo.SelectedIndex = 0;
            IssueDateFrom.SelectedDate = null;
            IssueDateTo.SelectedDate = null;
            await LoadIssueRecordsAsync();
        }

        private async void AddNewIssueButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditIssueWindow(_categories, _allProducts) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadProductsAndCategoriesAsync();
            if (result == true)
            {
                await LoadIssueRecordsAsync();
                await LoadStockReportAsync(); // issued qty affects Stock Report
            }
        }

        private async void EditIssueButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: IssueRecordLine line }) return;

            var header = await IssueRepository.GetHeaderAsync(line.Id);
            if (header == null)
            {
                MessageBox.Show("Could not find this issue record.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var items = await IssueRepository.GetItemsAsync(line.Id);

            var dialog = new AddEditIssueWindow(_categories, _allProducts, header, items) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadProductsAndCategoriesAsync();
            if (result == true)
            {
                await LoadIssueRecordsAsync();
                await LoadStockReportAsync();
            }
        }

        // private async void MarkDepositedButton_Click(object sender, RoutedEventArgs e)
        // {
        //     if (sender is not Button { DataContext: IssueRecordLine line }) return;

        //     var result = MessageBox.Show($"Mark {line.IssueCode} ({line.Item}) as deposited today?", "Confirm",
        //         MessageBoxButton.YesNo, MessageBoxImage.Question);
        //     if (result != MessageBoxResult.Yes) return;

        //     await IssueRepository.MarkDepositedAsync(line.Id, System.DateTime.Today);
        //     await LoadIssueRecordsAsync();
        // }

        private async void MarkDepositedButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: IssueRecordLine line }) return;

            var header = await IssueRepository.GetHeaderAsync(line.Id);
            if (header == null)
            {
                MessageBox.Show("Could not find this issue record.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var items = await IssueRepository.GetItemsForDepositAsync(line.Id);

            var dialog = new MarkDepositedWindow(header, items) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadIssueRecordsAsync();
                await LoadStockReportAsync();
            }
        }

        private async void IssueReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: IssueRecordLine line }) return;

            var header = await IssueRepository.GetHeaderAsync(line.Id);
            if (header == null)
            {
                MessageBox.Show("Could not find this issue record.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var items = await IssueRepository.GetItemsAsync(line.Id);

            string dateToDisplay = header.CreatedAtDisplay != "—"
                ? header.CreatedAtDisplay
                : (!string.IsNullOrWhiteSpace(line.CreatedAt) && line.CreatedAt != "—" ? line.CreatedAt : header.EventStartDateDisplay);

            var receipt = new ReceiptData
            {
                Type = "issue",
                BatchId = header.IssueCodeDisplay,
                IdLabel = "Issue Code",
                Subtitle = "Issue / Borrow — Movement Receipt",
                PersonName = header.ReceiverName,
                PersonNumber = header.ReceiverNumber,
                Items = items.Select(r => new ReceiptItem
                {
                    Name = r.ProductName,
                    Qty = decimal.TryParse(r.Qty, out var q) ? q : 0,
                    Category = r.Category ?? ""
                }).ToList(),
                Date = dateToDisplay,
                ExtraMeta = new List<ReceiptExtraField>
                {
                    new() { Label = "Event Name", Value = header.EventName },
                    new() { Label = "Receiver Position", Value = header.ReceiverPosition },
                    new() { Label = "Venue / Address", Value = header.VenueAddress },
                    new() { Label = "Expected Return Date", Value = header.DepositDateDisplay },
                    new() { Label = "Status", Value = header.Status },
                    new() { Label = "Remark", Value = header.Remark },
                }
            };

            new ReceiptWindow(receipt) { Owner = this }.ShowDialog();
        }

        private async void ExportIssueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allIssueRecords.Count == 0)
                {
                    MessageBox.Show("There's nothing to export yet — try clearing filters or date ranges first.",
                        "Nothing to export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var rows = new List<List<string>>();
                var distinctIds = _allIssueRecords.Select(r => r.Id).Distinct().ToList();

                foreach (var id in distinctIds)
                {
                    var header = await IssueRepository.GetHeaderAsync(id);
                    if (header == null) continue;

                    var line = _allIssueRecords.FirstOrDefault(r => r.Id == id);
                    string createdAtStr = line?.CreatedAtDisplay ?? "—";

                    var items = await IssueRepository.GetItemsAsync(id);

                    if (items != null && items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            var qty = decimal.TryParse(item.Qty, out var q) ? q : 0;
                            rows.Add(new List<string>
                            {
                                header.IssueCodeDisplay,
                                header.EventName,
                                string.IsNullOrWhiteSpace(item.Category) ? "—" : item.Category,
                                item.ProductName,
                                qty.ToString("N0"),
                                createdAtStr,
                                header.EventStartDateDisplay,
                                header.EventEndDateDisplay,
                                header.DepositDateDisplay,
                                string.IsNullOrWhiteSpace(header.VenueAddress) ? "—" : header.VenueAddress,
                                header.ReceiverName,
                                header.ReceiverNumber,
                                string.IsNullOrWhiteSpace(header.ReceiverPosition) ? "—" : header.ReceiverPosition,
                                header.Status,
                                header.DepositedOnDisplay,
                                string.IsNullOrWhiteSpace(header.Remark) ? "—" : header.Remark
                            });
                        }
                    }
                    else
                    {
                        rows.Add(new List<string>
                        {
                            header.IssueCodeDisplay,
                            header.EventName,
                            "—",
                            "—",
                            "0",
                            createdAtStr,
                            header.EventStartDateDisplay,
                            header.EventEndDateDisplay,
                            header.DepositDateDisplay,
                            string.IsNullOrWhiteSpace(header.VenueAddress) ? "—" : header.VenueAddress,
                            header.ReceiverName,
                            header.ReceiverNumber,
                            string.IsNullOrWhiteSpace(header.ReceiverPosition) ? "—" : header.ReceiverPosition,
                            header.Status,
                            header.DepositedOnDisplay,
                            string.IsNullOrWhiteSpace(header.Remark) ? "—" : header.Remark
                        });
                    }
                }

                string fileName = $"issue-goods-complete-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

                ExcelExportHelper.Export(
                    fileName,
                    "Issue Goods",
                    new List<string>
                    {
                        "Issue Code",
                        "Event Name",
                        "Category",
                        "Product / Item",
                        "Issued Qty",
                        "Created At",
                        "Event Start Date",
                        "Event End Date",
                        "Expected Deposit Date",
                        "Venue Address",
                        "Receiver Name",
                        "Receiver Mobile",
                        "Receiver Position",
                        "Status",
                        "Deposited On",
                        "Remarks"
                    },
                    rows
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export complete issue report.\n\n{ex.Message}", "Export Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= ADD DISTRIBUTION =================

        private List<DistributionSummary> _allDistributions = new();

        private async System.Threading.Tasks.Task LoadDistributionsAsync()
        {
            try
            {
                _allDistributions = await DistributionRepository.GetDistributionsAsync();
                ApplyDistributionSearchFilter();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load distributions.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyDistributionSearchFilter()
        {
            var query = DistributionSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            DistributionGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allDistributions
                : _allDistributions.Where(d =>
                    d.ShipmentId.ToLowerInvariant().Contains(query) ||
                    d.Destination.ToLowerInvariant().Contains(query) ||
                    d.ItemsList.ToLowerInvariant().Contains(query)
                  ).ToList();
        }

        private void DistributionSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyDistributionSearchFilter();

        private async void AddDistributionButton_Click(object sender, RoutedEventArgs e)
        {
            var officeNames = (await OfficeRepository.GetOfficesAsync()).Select(o => o.Name).ToList();
            var dialog = new AddEditDistributionWindow(_categories, _allProducts, officeNames) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadProductsAndCategoriesAsync();
            if (result == true)
            {
                await LoadDistributionsAsync();
                await LoadFixedAssetsAsync();
                if (dialog.CreatedReceipt != null)
                {
                    new ReceiptWindow(dialog.CreatedReceipt) { Owner = this }.ShowDialog();
                }
            }
        }

        private async void EditDistributionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DistributionSummary distribution }) return;

            var officeNames = (await OfficeRepository.GetOfficesAsync()).Select(o => o.Name).ToList();
            var items = await DistributionRepository.GetItemsAsync(distribution.Id);

            var dialog = new AddEditDistributionWindow(_categories, _allProducts, officeNames, distribution, items) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadProductsAndCategoriesAsync();
            if (result == true)
            {
                await LoadDistributionsAsync();
                await LoadFixedAssetsAsync();
            }
        }

        private async void DistributionReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DistributionSummary distribution }) return;

            var items = await DistributionRepository.GetItemsAsync(distribution.Id);

            var receipt = new ReceiptData
            {
                Type = "dispatched",
                BatchId = distribution.ShipmentId,
                IdLabel = "Shipment ID",
                Subtitle = "Export — Distribution Receipt",
                PersonName = distribution.ReceiverName,
                PersonNumber = distribution.ReceiverNumber,
                Items = items.Select(r => new ReceiptItem { Name = r.ProductName, Qty = decimal.TryParse(r.Qty, out var q) ? q : 0 }).ToList(),
                Date = distribution.DateDisplay,
                ExtraMeta = new List<ReceiptExtraField>
                {
                    new() { Label = "Destination", Value = distribution.Destination },
                    new() { Label = "Position", Value = distribution.Position },
                    new() { Label = "Remark", Value = distribution.Remark },
                }
            };

            new ReceiptWindow(receipt) { Owner = this }.ShowDialog();
        }

        private void ExportDistributionButton_Click(object sender, RoutedEventArgs e)
        {
            var rows = new List<List<string>>();
            foreach (var d in DistributionGrid.ItemsSource?.Cast<DistributionSummary>() ?? Enumerable.Empty<DistributionSummary>())
            {
                rows.Add(new List<string> { d.ShipmentId, d.ItemsList, d.TotalQtyDisplay, d.DateDisplay, d.Destination });
            }
            // Current date + current time
            string fileName = $"distributions-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

            ExcelExportHelper.Export(fileName, "Distributions",
                new List<string> { "Shipment ID", "Item(s)", "Quantity", "Dispatch Date", "Destination" }, rows);
        }

        // ================= ADD PURCHASE =================

        private List<Purchase> _allPurchases = new();
        private string? _purchaseFilterVendor, _purchaseFilterProduct;
        private System.DateTime? _purchaseFilterFrom, _purchaseFilterTo;

        private async System.Threading.Tasks.Task LoadPurchasesAsync()
        {
            try
            {
                // _allPurchases = await PurchaseRepository.GetPurchasesAsync(
                //     _purchaseFilterVendor, _purchaseFilterProduct, _purchaseFilterFrom, _purchaseFilterTo);
                // ApplyPurchaseSearchFilter();

                _allPurchases = await PurchaseRepository.GetPurchasesAsync(
                   _purchaseFilterVendor, _purchaseFilterProduct, _purchaseFilterFrom, _purchaseFilterTo);

                // Compute each row's "other linked challans" text -- depends on sibling rows
                // sharing the same InvoiceGroupId, so it's done here rather than as a pure property.
                foreach (var p in _allPurchases)
                {
                    if (p.InvoiceGroupId.HasValue)
                    {
                        var others = _allPurchases
                            .Where(x => x.InvoiceGroupId == p.InvoiceGroupId && x.Id != p.Id && !string.IsNullOrWhiteSpace(x.ChallanNumber))
                            .Select(x => x.ChallanNumber!)
                            .ToList();
                        p.LinkedChallansDisplay = others.Count > 0 ? $"Challans: {string.Join(", ", others)}" : "";
                    }
                }

                ApplyPurchaseSearchFilter();

                var count = new[] { _purchaseFilterVendor, _purchaseFilterProduct, _purchaseFilterFrom?.ToString(), _purchaseFilterTo?.ToString() }
                    .Count(v => !string.IsNullOrEmpty(v));
                PurchaseFilterButton.Content = count > 0 ? $"Filter ({count})" : "Filter";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load purchases.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CombineChallansButton_Click(object sender, RoutedEventArgs e)
        {
            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var dialog = new CombineChallansWindow(vendorNames) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadPurchasesAsync();
            }
        }

        private void ApplyPurchaseSearchFilter()
        {
            var query = PurchaseSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            PurchaseGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allPurchases
                : _allPurchases.Where(p =>
                    p.VendorName.ToLowerInvariant().Contains(query) ||
                    (p.PoReference?.ToLowerInvariant().Contains(query) ?? false) ||
                    (p.InvoiceNumber?.ToLowerInvariant().Contains(query) ?? false)
                  ).ToList();
        }

        private void PurchaseSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyPurchaseSearchFilter();

        private async void PurchaseFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var productNames = _allProducts.Select(p => p.Name).ToList();

            var dialog = new PoFilterWindow(vendorNames, productNames, _purchaseFilterVendor, _purchaseFilterProduct, _purchaseFilterFrom, _purchaseFilterTo)
            { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _purchaseFilterVendor = dialog.SelectedVendor;
                _purchaseFilterProduct = dialog.SelectedProduct;
                _purchaseFilterFrom = dialog.SelectedDateFrom;
                _purchaseFilterTo = dialog.SelectedDateTo;
                await LoadPurchasesAsync();
            }
        }

        private async void AddPurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var officeNames = (await OfficeRepository.GetOfficesAsync()).Select(o => o.Name).ToList();
            var poReferences = (await PurchaseOrderRepository.GetOrdersAsync(null, null, null, null)).Select(po => po.PoReference).ToList();
            var dialog = new AddEditPurchaseWindow(vendorNames, _categories, _allProducts, officeNames, poReferences: poReferences) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadVendorsAsync(); // a new vendor may have been added inline
                await LoadPurchasesAsync();
                await LoadFixedAssetsAsync(); // a new Direct purchase may have added a distribution
                if (dialog.SavedPurchaseId.HasValue)
                {
                    await ShowPurchaseReceiptAsync(dialog.SavedPurchaseId.Value);
                }
            }
        }

        private async void EditPurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Purchase purchase }) return;

            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var officeNames = (await OfficeRepository.GetOfficesAsync()).Select(o => o.Name).ToList();
            var poReferences = (await PurchaseOrderRepository.GetOrdersAsync(null, null, null, null)).Select(po => po.PoReference).ToList();
            var items = await PurchaseRepository.GetItemsAsync(purchase.Id);
            var invoiceFiles = await PurchaseRepository.GetInvoiceFilesAsync(purchase.Id);

            (string?, string?, System.DateTime?, string?, string?, string?, string?, List<(string?, string, decimal)>)? distribution = null;
            if (purchase.DistributionId.HasValue)
            {
                distribution = await PurchaseRepository.GetDistributionWithItemsAsync(purchase.DistributionId.Value);
            }

            var dialog = new AddEditPurchaseWindow(vendorNames, _categories, _allProducts, officeNames, purchase, items, distribution, invoiceFiles, poReferences) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadVendorsAsync(); // Always sync vendors in real time
            if (result == true)
            {
                await LoadPurchasesAsync();
                await LoadFixedAssetsAsync();
            }
        }

        private async void PurchaseReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Purchase purchase }) return;
            await ShowPurchaseReceiptAsync(purchase.Id);
        }

        private async System.Threading.Tasks.Task ShowPurchaseReceiptAsync(int purchaseId)
        {
            var purchase = _allPurchases.FirstOrDefault(p => p.Id == purchaseId);
            if (purchase == null)
            {
                // Freshly added purchase may not be in the cached list yet if the list hasn't reloaded.
                _allPurchases = await PurchaseRepository.GetPurchasesAsync(null, null, null, null);
                purchase = _allPurchases.FirstOrDefault(p => p.Id == purchaseId);
                if (purchase == null) return;
            }

            var items = await PurchaseRepository.GetItemsAsync(purchase.Id);

            (string?, string?, System.DateTime?, string?, string?, string?, string?, List<(string?, string, decimal)>)? distribution = null;
            if (purchase.DistributionId.HasValue)
            {
                distribution = await PurchaseRepository.GetDistributionWithItemsAsync(purchase.DistributionId.Value);
            }

            new PurchaseReceiptWindow(purchase, items, distribution) { Owner = this }.ShowDialog();
        }

        private void ExportPurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var menu = new ContextMenu();

            var itemDetailed = new MenuItem
            {
                Header = "📦 Detailed Itemized Purchase Report (with Products & Items)",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 6, 12, 6)
            };
            itemDetailed.Click += async (s, args) => await ExportItemizedPurchasesAsync();

            var itemSummary = new MenuItem
            {
                Header = "📋 Purchase Summary Report (Header Level)",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 6, 12, 6)
            };
            itemSummary.Click += (s, args) => ExportSummaryPurchases();

            menu.Items.Add(itemDetailed);
            menu.Items.Add(itemSummary);

            menu.PlacementTarget = btn;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private async System.Threading.Tasks.Task ExportItemizedPurchasesAsync()
        {
            try
            {
                var purchases = PurchaseGrid.ItemsSource?.Cast<Purchase>().ToList() ?? new List<Purchase>();
                if (purchases.Count == 0)
                {
                    MessageBox.Show("There's nothing to export yet — try clearing filters or search first.",
                        "Nothing to export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var rows = new List<List<string>>();

                foreach (var p in purchases)
                {
                    var items = await PurchaseRepository.GetItemsAsync(p.Id);
                    if (items != null && items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            var qty = decimal.TryParse(item.Qty, out var q) ? q : 0;
                            var price = decimal.TryParse(item.Price, out var pr) ? pr : 0;
                            var gst = item.GstPercent;
                            var subtotal = qty * price;
                            var gstAmt = subtotal * (gst / 100m);
                            var lineTotal = item.TotalValue > 0 ? item.TotalValue : (subtotal + gstAmt);

                            rows.Add(new List<string>
                            {
                                p.PoReferenceDisplay,
                                p.InvoiceNumberDisplay,
                                p.PurchaseType,
                                p.VendorName,
                                string.IsNullOrWhiteSpace(item.Category) ? "—" : item.Category,
                                item.ProductName,
                                qty.ToString("N0"),
                                $"₹{price:N2}",
                                $"{gst:G29}%",
                                $"₹{lineTotal:N2}",
                                p.PaymentModeDisplay,
                                p.PurchaseDateDisplay
                            });
                        }
                    }
                    else
                    {
                        rows.Add(new List<string>
                        {
                            p.PoReferenceDisplay,
                            p.InvoiceNumberDisplay,
                            p.PurchaseType,
                            p.VendorName,
                            "—",
                            "—",
                            "—",
                            "—",
                            "—",
                            p.TotalDisplay,
                            p.PaymentModeDisplay,
                            p.PurchaseDateDisplay
                        });
                    }
                }

                string fileName = $"purchases-itemized-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

                ExcelExportHelper.Export(
                    fileName,
                    "Itemized Purchases",
                    new List<string>
                    {
                        "PO Reference",
                        "Invoice Number",
                        "Purchase Type",
                        "Vendor Name",
                        "Category",
                        "Product Name",
                        "Qty",
                        "Unit Price",
                        "GST %",
                        "Line Total",
                        "Payment Mode",
                        "Purchase Date"
                    },
                    rows
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export purchase items.\n\n{ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSummaryPurchases()
        {
            var rows = new List<List<string>>();
            foreach (var p in PurchaseGrid.ItemsSource?.Cast<Purchase>() ?? Enumerable.Empty<Purchase>())
            {
                rows.Add(new List<string>
                {
                    p.PoReferenceDisplay,
                    p.InvoiceNumberDisplay,
                    p.PurchaseType,
                    p.VendorName,
                    p.TotalDisplay,
                    p.PaymentModeDisplay,
                    p.PurchaseDateDisplay
                });
            }

            string fileName = $"purchases-summary-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

            ExcelExportHelper.Export(
                fileName,
                "Purchases Summary",
                new List<string> { "PO Reference", "Invoice Number", "Type", "Vendor", "Total Amount", "Payment Mode", "Date" },
                rows
            );
        }

        // ================= RAISE PO =================

        private List<PurchaseOrder> _allPurchaseOrders = new();
        private List<PurchaseOrderItemDisplayRow> _allPoDisplayRows = new();
        private string? _poFilterVendor, _poFilterProduct;
        private System.DateTime? _poFilterFrom, _poFilterTo;

        private async System.Threading.Tasks.Task LoadPurchaseOrdersAsync()
        {
            try
            {
                _allPurchaseOrders = await PurchaseOrderRepository.GetOrdersAsync(
                    _poFilterVendor, _poFilterProduct, _poFilterFrom, _poFilterTo);

                var displayRows = new List<PurchaseOrderItemDisplayRow>();

                foreach (var po in _allPurchaseOrders)
                {
                    var items = await PurchaseOrderRepository.GetItemsAsync(po.Id);
                    if (items != null && items.Count > 0)
                    {
                        foreach (var item in items)
                        {
                            var qty = decimal.TryParse(item.Qty, out var q) ? q : 0;
                            var price = decimal.TryParse(item.Price, out var p) ? p : 0;
                            var gst = item.GstPercent;
                            var subtotal = qty * price;
                            var lineTotal = item.TotalValue > 0 ? item.TotalValue : (subtotal * (1 + gst / 100m));

                            displayRows.Add(new PurchaseOrderItemDisplayRow
                            {
                                PurchaseOrderId = po.Id,
                                OriginalPo = po,
                                PoReference = po.PoReference,
                                VendorName = po.VendorName,
                                Category = item.Category,
                                ItemName = item.ProductName,
                                Qty = qty,
                                UnitPrice = price,
                                GstPercent = gst,
                                LineTotal = lineTotal,
                                CreatedDate = po.CreatedDate,
                                Delivery = po.Delivery
                            });
                        }
                    }
                    else
                    {
                        displayRows.Add(new PurchaseOrderItemDisplayRow
                        {
                            PurchaseOrderId = po.Id,
                            OriginalPo = po,
                            PoReference = po.PoReference,
                            VendorName = po.VendorName,
                            Category = "—",
                            ItemName = po.ItemsSummaryDisplay,
                            Qty = po.TotalQty,
                            UnitPrice = 0,
                            GstPercent = 0,
                            LineTotal = 0,
                            CreatedDate = po.CreatedDate,
                            Delivery = po.Delivery
                        });
                    }
                }

                _allPoDisplayRows = displayRows;
                ApplyPoSearchFilter();

                var count = new[] { _poFilterVendor, _poFilterProduct, _poFilterFrom?.ToString(), _poFilterTo?.ToString() }
                    .Count(v => !string.IsNullOrEmpty(v));
                PoFilterButton.Content = count > 0 ? $"Filter ({count})" : "Filter";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load purchase orders.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyPoSearchFilter()
        {
            var query = PoSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            PoGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allPoDisplayRows
                : _allPoDisplayRows.Where(row =>
                    row.PoReference.ToLowerInvariant().Contains(query) ||
                    row.VendorName.ToLowerInvariant().Contains(query) ||
                    row.ItemName.ToLowerInvariant().Contains(query) ||
                    (row.Category?.ToLowerInvariant().Contains(query) ?? false)
                  ).ToList();
        }

        private void PoSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyPoSearchFilter();

        private async void PoFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var productNames = _allProducts.Select(p => p.Name).ToList();

            var dialog = new PoFilterWindow(vendorNames, productNames, _poFilterVendor, _poFilterProduct, _poFilterFrom, _poFilterTo)
            { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _poFilterVendor = dialog.SelectedVendor;
                _poFilterProduct = dialog.SelectedProduct;
                _poFilterFrom = dialog.SelectedDateFrom;
                _poFilterTo = dialog.SelectedDateTo;
                await LoadPurchaseOrdersAsync();
            }
        }

        private async void AddPoButton_Click(object sender, RoutedEventArgs e)
        {
            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var dialog = new AddEditPoWindow(vendorNames, _categories, _allProducts) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadVendorsAsync();
            await LoadProductsAndCategoriesAsync();
            if (result == true)
            {
                await LoadPurchaseOrdersAsync();
            }
        }

        private async void EditPoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            PurchaseOrder? po = btn.DataContext as PurchaseOrder;
            if (po == null && btn.DataContext is PurchaseOrderItemDisplayRow displayRow)
            {
                po = displayRow.OriginalPo;
            }
            if (po == null) return;

            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var items = await PurchaseOrderRepository.GetItemsAsync(po.Id);

            var dialog = new AddEditPoWindow(vendorNames, _categories, _allProducts, po, items) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadVendorsAsync();
            await LoadProductsAndCategoriesAsync();
            if (result == true)
            {
                await LoadPurchaseOrdersAsync();
            }
        }

        private void WhatsAppPoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            PurchaseOrder? po = btn.DataContext as PurchaseOrder;
            if (po == null && btn.DataContext is PurchaseOrderItemDisplayRow displayRow)
            {
                po = displayRow.OriginalPo;
            }
            if (po == null) return;

            MessageBox.Show($"WhatsApp message sent to {po.VendorName}", "Sent",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void PrintPoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            PurchaseOrder? po = btn.DataContext as PurchaseOrder;
            if (po == null && btn.DataContext is PurchaseOrderItemDisplayRow displayRow)
            {
                po = displayRow.OriginalPo;
            }
            if (po == null) return;

            var items = await PurchaseOrderRepository.GetItemsAsync(po.Id);
            new PurchaseOrderPrintWindow(po, items) { Owner = this }.ShowDialog();
        }

        private void ExportPoButton_Click(object sender, RoutedEventArgs e)
        {
            var rows = new List<List<string>>();
            var currentRows = PoGrid.ItemsSource?.Cast<PurchaseOrderItemDisplayRow>() ?? Enumerable.Empty<PurchaseOrderItemDisplayRow>();

            foreach (var row in currentRows)
            {
                rows.Add(new List<string>
                {
                    row.PoReferenceDisplay,
                    row.VendorName,
                    row.CategoryDisplay,
                    row.ItemName,
                    row.QtyDisplay,
                    row.UnitPriceDisplay,
                    row.GstDisplay,
                    row.LineTotalDisplay,
                    row.CreatedDateDisplay,
                    row.DeliveryDisplay
                });
            }

            string fileName = $"purchase-orders-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

            ExcelExportHelper.Export(
                fileName,
                "Purchase Orders",
                new List<string>
                {
                    "PO Reference",
                    "Vendor",
                    "Category",
                    "Product / Item",
                    "Quantity",
                    "Unit Price",
                    "GST %",
                    "Total Amount",
                    "Generated On",
                    "Expected Delivery"
                },
                rows
            );
        }

        // ================= DAMAGE =================

        private List<DamageRecord> _allDamageRecords = new();

        // private async System.Threading.Tasks.Task LoadDamageAsync()
        // {
        //     try
        //     {
        //         _allDamageRecords = await DamageRepository.GetRecordsAsync(DamageDateFrom.SelectedDate, DamageDateTo.SelectedDate);
        //         ApplyDamageSearchFilter();
        //     }
        //     catch (System.Exception ex)
        //     {
        //         MessageBox.Show($"Could not load damage records.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //     }
        // }

        private List<DamageRecord> _allDamage = new();

        private async System.Threading.Tasks.Task LoadDamageAsync()
        {
            try
            {
                _allDamage = await DamageRepository.GetRecordsAsync(DamageDateFrom.SelectedDate, DamageDateTo.SelectedDate);
                _allDamageRecords = _allDamage;
                DamageGrid.ItemsSource = _allDamage;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load damage records.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyDamageSearchFilter()
        {
            var query = DamageSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            DamageGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allDamageRecords
                : _allDamageRecords.Where(r =>
                    (r.DamageCode?.ToLowerInvariant().Contains(query) ?? false) ||
                    (r.CategoryDisplay?.ToLowerInvariant().Contains(query) ?? false) ||
                    (r.ItemName?.ToLowerInvariant().Contains(query) ?? false) ||
                    (r.ReporteeName?.ToLowerInvariant().Contains(query) ?? false)
                  ).ToList();
        }

        private void DamageSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyDamageSearchFilter();

        private async void DamageDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => await LoadDamageAsync();

        private async void ClearDamageDatesButton_Click(object sender, RoutedEventArgs e)
        {
            DamageDateFrom.SelectedDate = null;
            DamageDateTo.SelectedDate = null;
            await LoadDamageAsync();
        }

        private async void AddDamageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddDamageWindow(_categories, _allProducts) { Owner = this };
            var result = dialog.ShowDialog();
            await LoadProductsAndCategoriesAsync(); // Always reload categories and products in real time
            if (result == true)
            {
                await LoadDamageAsync();
                if (dialog.CreatedReceipt != null)
                {
                    new ReceiptWindow(dialog.CreatedReceipt) { Owner = this }.ShowDialog();
                }
            }
        }

        private void ViewDamageDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DamageRecord record }) return;

            new DamageDetailsWindow(record) { Owner = this }.ShowDialog();
        }

        private void PrintDamageReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DamageRecord record }) return;

            var receipt = new ReceiptData
            {
                Type = "damage",
                BatchId = record.DamageCode,
                IdLabel = "Damage ID",
                Subtitle = "Damage — Report Receipt",
                PersonName = record.ReporteeName,
                PersonNumber = record.ReporteeNumber,
                Items = new List<ReceiptItem> { new() { Name = record.ItemName, Qty = record.Qty, Category = record.Category ?? "" } },
                Date = record.DateDisplay,
                ExtraMeta = new List<ReceiptExtraField>
                {
                    new() { Label = "Reportee Position", Value = record.ReporteePosition },
                    new() { Label = "Remark", Value = record.Remark },
                }
            };

            new ReceiptWindow(receipt) { Owner = this }.ShowDialog();
        }

        // private void ExportDamageButton_Click(object sender, RoutedEventArgs e)
        // {
        //     var rows = new List<List<string>>();
        //     foreach (var d in DamageGrid.ItemsSource?.Cast<DamageRecord>() ?? Enumerable.Empty<DamageRecord>())
        //     {
        //         rows.Add(new List<string> { d.DamageCode, d.CategoryDisplay, d.ItemName, d.QtyDisplay, d.DateDisplay, d.RemarkDisplay });
        //     }
        //     ExcelExportHelper.Export("damage-register.xlsx", "Damage",
        //         new List<string> { "Damage ID", "Category", "Item", "Qty", "Date", "Remark" }, rows);
        // }
        private void ExportDamageButton_Click(object sender, RoutedEventArgs e)
        {
            var rows = new List<List<string>>();

            foreach (var d in DamageGrid.ItemsSource?.Cast<DamageRecord>()
                     ?? Enumerable.Empty<DamageRecord>())
            {
                rows.Add(new List<string>
        {
            d.DamageCode,
            d.CategoryDisplay,
            d.ItemName,
            d.QtyDisplay,
            d.DateDisplay,
            d.RemarkDisplay
        });
            }

            // Current date + current time
            string fileName = $"damage-register-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

            ExcelExportHelper.Export(
                fileName,
                "Damage",
                new List<string>
                {
            "Damage ID",
            "Category",
            "Item",
            "Qty",
            "Date",
            "Remark"
                },
                rows
            );
        }


        // ================= STOCK REPORT =================

        private List<StockReportItem> _allStockReport = new();

        private async System.Threading.Tasks.Task LoadStockReportAsync()
        {
            try
            {
                _allStockReport = await StockReportRepository.GetReportAsync(
                    StockReportDateFrom.SelectedDate, StockReportDateTo.SelectedDate);
                ApplyStockReportFilter();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load the stock report.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyStockReportFilter()
        {
            var query = StockReportSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            StockReportGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allStockReport
                : _allStockReport.Where(r => r.Item.ToLowerInvariant().Contains(query)).ToList();
        }

        private void StockReportSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyStockReportFilter();

        private async void StockReportDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => await LoadStockReportAsync();

        private async void RefreshStockReportButton_Click(object sender, RoutedEventArgs e) => await LoadStockReportAsync();

        private async void ClearStockReportDatesButton_Click(object sender, RoutedEventArgs e)
        {
            StockReportDateFrom.SelectedDate = null;
            StockReportDateTo.SelectedDate = null;
            await LoadStockReportAsync();
        }

        private void StockReportItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: StockReportItem item }) return;

            new ItemLedgerWindow(item.Item) { Owner = this }.ShowDialog();
        }

        // private void ExportStockReportButton_Click(object sender, RoutedEventArgs e)
        // {
        //     var rows = new List<List<string>>();
        //     foreach (var s in StockReportGrid.ItemsSource?.Cast<StockReportItem>() ?? Enumerable.Empty<StockReportItem>())
        //     {
        //         rows.Add(new List<string> { s.Item, s.Batches.ToString(), s.TotalQtyDisplay, s.DispatchedDisplay, s.RemainingDisplay });
        //     }
        //     ExcelExportHelper.Export("stock-report.xlsx", "Stock Report",
        //         new List<string> { "Item", "Batches", "Total Qty", "Dispatched", "Remaining" }, rows);
        // }

        private void ExportStockReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var menu = new ContextMenu();

            var itemComplete = new MenuItem
            {
                Header = "📋 Complete Option",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 6, 12, 6)
            };
            itemComplete.Click += (s, args) => ExportCompleteStockReport();

            var itemProductLedger = new MenuItem
            {
                Header = "📦 Particular Product Ledger Report",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 6, 12, 6)
            };
            itemProductLedger.Click += async (s, args) => await ExportParticularProductLedgerReportAsync();

            menu.Items.Add(itemComplete);
            menu.Items.Add(itemProductLedger);

            menu.PlacementTarget = btn;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void ExportCompleteStockReport()
        {
            var rows = new List<List<string>>();
            foreach (var s in StockReportGrid.ItemsSource?.Cast<StockReportItem>() ?? Enumerable.Empty<StockReportItem>())
            {
                rows.Add(new List<string> { s.Item, s.Batches.ToString(), s.TotalQtyDisplay, s.DispatchedDisplay, s.DamageQtyDisplay, s.IssueQtyDisplay, s.RemainingDisplay });
            }

            string fileName = $"stock-report-complete-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

            ExcelExportHelper.Export(fileName, "Stock Report",
                new List<string> { "Item", "Entries", "Total Qty", "Dispatched", "Damage Qty", "Issue Qty", "Remaining" }, rows);
        }

        private async System.Threading.Tasks.Task ExportParticularProductLedgerReportAsync()
        {
            try
            {
                var productNames = _allStockReport.Select(s => s.Item).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
                if (productNames.Count == 0)
                {
                    productNames = _allProducts.Select(p => p.Name).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
                }

                if (productNames.Count == 0)
                {
                    MessageBox.Show("There are no products available to export.", "No Products", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SelectProductExportDialog(productNames) { Owner = this };
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedProduct))
                {
                    string productName = dialog.SelectedProduct;
                    var entries = await StockReportRepository.GetProductLedgerAsync(productName);

                    if (entries.Count == 0)
                    {
                        MessageBox.Show($"No movement ledger entries found for product '{productName}'.", "No Ledger Entries", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var rows = new List<List<string>>();
                    foreach (var entry in entries)
                    {
                        rows.Add(new List<string>
                        {
                            entry.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                            entry.Type ?? "",
                            entry.BatchCode ?? "",
                            entry.QtyDisplay,
                            entry.Party ?? ""
                        });
                    }

                    char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
                    string safeProductName = string.Concat(productName.Select(c => invalidChars.Contains(c) ? '_' : c));
                    string fileName = $"stock-ledger-{safeProductName}-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";

                    ExcelExportHelper.Export(fileName, $"{productName} Ledger",
                        new List<string> { "Date & Time", "Type / Movement", "Reference Code", "Quantity", "Party / Destination" }, rows);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not export product ledger.\n\n{ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= FIXED ASSETS =================

        private List<FixedAssetSummary> _allAssets = new();

        private async System.Threading.Tasks.Task LoadFixedAssetsAsync()
        {
            try
            {
                _allAssets = await FixedAssetRepository.GetSummaryAsync();

                var totalQty = _allAssets.Sum(a => a.Qty);
                var totalValue = _allAssets.Sum(a => a.Value);
                AssetsTotalQtyText.Text = totalQty.ToString("N0");
                AssetsTotalValueText.Text = FormatAssetValue(totalValue);

                ApplyAssetsFilter();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load fixed assets.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string FormatAssetValue(decimal value)
        {
            if (value >= 10000000m) return $"₹{System.Math.Round(value / 10000000m, 2)} Cr";
            if (value >= 100000m) return $"₹{System.Math.Round(value / 100000m, 2)} L";
            return $"₹{value:N0}";
        }

        private void ApplyAssetsFilter()
        {
            var query = AssetsSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            AssetsGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allAssets
                : _allAssets.Where(a =>
                    a.Category.ToLowerInvariant().Contains(query) ||
                    a.Item.ToLowerInvariant().Contains(query) ||
                    a.Code.ToLowerInvariant().Contains(query)
                  ).ToList();
        }

        private void AssetsSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyAssetsFilter();

        private void AssetItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: FixedAssetSummary asset }) return;

            new AssetDistributionWindow(asset.Item) { Owner = this }.ShowDialog();
        }

        // ================= FOOD PACKETS =================

        // private async System.Threading.Tasks.Task LoadBatchesAsync()
        // {
        //     try
        //     {
        //         var batches = await FoodPacketRepository.GetBatchesAsync();
        //         BatchesGrid.ItemsSource = batches;
        //     }
        //     catch (System.Exception ex)
        //     {
        //         MessageBox.Show($"Could not load batches.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //     }
        // }

        private List<FoodPacketBatch> _allBatches = new();

        private async System.Threading.Tasks.Task LoadBatchesAsync()
        {
            try
            {
                _allBatches = await FoodPacketRepository.GetBatchesAsync();
                BatchesGrid.ItemsSource = _allBatches;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load batches.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void NewBatchButton_Click(object sender, RoutedEventArgs e)
        {
            var vendorNames = _allVendors.Select(v => v.Name).ToList();
            var dialog = new NewBatchWindow(vendorNames) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadBatchesAsync();
                if (dialog.CreatedReceipt != null)
                {
                    new ReceiptWindow(dialog.CreatedReceipt) { Owner = this }.ShowDialog();
                }
            }
        }

        private void LedgerButton_Click(object sender, RoutedEventArgs e)
        {
            new LedgerWindow { Owner = this }.ShowDialog();
        }

        private async void DispatchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: FoodPacketBatch batch }) return;

            var dialog = new DispatchWindow(batch) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadBatchesAsync();
                if (dialog.CreatedReceipt != null)
                {
                    new ReceiptWindow(dialog.CreatedReceipt) { Owner = this }.ShowDialog();
                }
            }
        }

        private void ViewDispatchesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: FoodPacketBatch batch }) return;

            new DispatchHistoryWindow(batch) { Owner = this }.ShowDialog();
        }

        // ================= VENDORS =================

        private async System.Threading.Tasks.Task LoadVendorsAsync()
        {
            try
            {
                _allVendors = await VendorRepository.GetVendorsAsync();
                ApplyVendorFilter();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load vendors.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyVendorFilter()
        {
            var query = VendorSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            VendorsGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allVendors
                : _allVendors.Where(v =>
                    (v.Name?.ToLowerInvariant().Contains(query) ?? false) ||
                    (v.ContactPerson?.ToLowerInvariant().Contains(query) ?? false) ||
                    (v.Email?.ToLowerInvariant().Contains(query) ?? false) ||
                    (v.ContactNumber?.Contains(query) ?? false)
                  ).ToList();
        }

        private void VendorSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyVendorFilter();

        private async void AddVendorButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditVendorWindow { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadVendorsAsync();
            }
        }

        private async void EditVendorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Vendor vendor }) return;

            var dialog = new AddEditVendorWindow(vendor) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadVendorsAsync();
            }
        }

        private void ViewVendorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Vendor vendor }) return;

            new VendorDetailsWindow(vendor) { Owner = this }.ShowDialog();
        }

        private void ExportVendorsButton_Click(object sender, RoutedEventArgs e)
        {
            var rows = new List<List<string>>();
            foreach (var v in VendorsGrid.ItemsSource?.Cast<Vendor>() ?? Enumerable.Empty<Vendor>())
            {
                rows.Add(new List<string> { v.Name, v.Gst ?? "", v.ContactPerson ?? "", v.ContactNumber ?? "", v.Email ?? "", v.Address ?? "", v.BankHolder ?? "", v.BankAccount ?? "", v.BankIfsc ?? "", v.BankBranch ?? "", v.CreatedAt });
            }
            // Current date + current time
            string fileName = $"vendors-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Vendors",
                new List<string> { "Vendor Name", "GST", "Contact Person", "Number", "Email", "Address", "Bank Holder", "Account", "IFSC Code", "Branch", "CreatedAt" }, rows);
        }

        // ================= PRODUCTS =================

        private async System.Threading.Tasks.Task LoadProductsAndCategoriesAsync()
        {
            try
            {
                _categories = await ProductRepository.GetCategoriesAsync();
                _allProducts = await ProductRepository.GetProductsAsync();
                ApplyProductFilter();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load products.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyProductFilter()
        {
            var query = ProductSearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            ProductsGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _allProducts
                : _allProducts.Where(p =>
                    (p.Name?.ToLowerInvariant().Contains(query) ?? false) ||
                    (p.ProductCode?.ToLowerInvariant().Contains(query) ?? false) ||
                    (p.Category?.ToLowerInvariant().Contains(query) ?? false)
                  ).ToList();
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyProductFilter();

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditProductWindow(_categories) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadProductsAndCategoriesAsync();
            }
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Product product }) return;

            var dialog = new AddEditProductWindow(_categories, product) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await LoadProductsAndCategoriesAsync();
            }
        }

        private async void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: Product product } || !product.Id.HasValue)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{product.Name}'?",
                "Delete Product",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Call ProductRepository instead of _allProducts
                await ProductRepository.DeleteProductAsync(product.Id.Value);

                // Refresh products and dashboard metrics
                await LoadProductsAndCategoriesAsync();
                BuildDashboardOverview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete product.\n\n{ex.Message}", "Delete Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportProductsButton_Click(object sender, RoutedEventArgs e)
        {
            var rows = new List<List<string>>();
            foreach (var p in ProductsGrid.ItemsSource?.Cast<Product>() ?? Enumerable.Empty<Product>())
            {
                rows.Add(new List<string> { p.ProductCode, p.Name, p.Category, p.Unit, p.MinQtyDisplay, p.AvgPriceDisplay, p.CreatedAt });
            }
            // Current date + current time
            string fileName = $"products-{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.xlsx";
            ExcelExportHelper.Export(fileName, "Products",
                new List<string> { "Product Code", "Product Name", "Category", "Unit", "Min Qty", "Avg Price", "CreatedAt" }, rows);
        }

        public void SetUser(string displayName)
        {
            displayName = string.IsNullOrWhiteSpace(displayName) ? "User" : displayName.Trim();
            UserNameText.Text = displayName;

            var parts = displayName.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            string initials = parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}"
                : displayName.Length >= 2 ? displayName[..2] : displayName;
            UserInitialsText.Text = initials.ToUpperInvariant();
        }

        private void NavItem_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton toggle || toggle.Tag is not string rawKey) return;
            var key = rawKey.ToLowerInvariant();

            foreach (var other in new[]
                     {
                         NavDashboard, NavFoodPackets, NavFixedAssets, NavStockReport, NavDamage, NavIssueBorrow,
                         NavRaisePo, NavAddPurchase, NavQuotation, NavAddDistribution, NavIssueBorrow, NavAddVendor,
                         NavAddProduct, NavUtilityReminder, NavReports
                     })
            {
                if (!ReferenceEquals(other, toggle)) other.IsChecked = false;
            }

            foreach (var panel in new[]
                     {
                         PanelDashboard, PanelFoodPackets, PanelFixedAssets, PanelStockReport, PanelDamage, PanelIssueBorrow,
                         PanelRaisePo, PanelAddPurchase, PanelQuotation, PanelAddDistribution, PanelAddVendor,
                         PanelAddProduct, PanelUtilityReminder, PanelReports
                     })
            {
                panel.Visibility = Visibility.Collapsed;
            }

            var targetPanel = key switch
            {
                "dashboard" => PanelDashboard,
                "food-packets" => PanelFoodPackets,
                "fixed-assets" => PanelFixedAssets,
                "stock-report" => PanelStockReport,
                "damage" => PanelDamage,
                "issue-borrow" => PanelIssueBorrow,
                "raise-po" => PanelRaisePo,
                "add-purchase" => PanelAddPurchase,
                "quotation" => PanelQuotation,
                "add-export" => PanelAddDistribution,
                "add-vendor" => PanelAddVendor,
                "add-product" => PanelAddProduct,
                "utility-reminder" => PanelUtilityReminder,
                "reports" => PanelReports,
                _ => PanelDashboard
            };
            targetPanel.Visibility = Visibility.Visible;

            PanelTitleText.Text = TitleMap.TryGetValue(key, out var title) ? title : key;

            if (key == "dashboard")
            {
                BuildDashboardOverview();
            }

            if (key == "stock-report")
            {
                _ = LoadStockReportAsync();
            }

            if (key == "issue-borrow")
            {
                _ = LoadIssueRecordsAsync();
            }

            if (key == "quotation")
            {
                _ = LoadProjectsAsync();
            }

            if (key == "utility-reminder")
            {
                _ = LoadUtilityRemindersAsync();
            }

            if (key == "reports")
            {
                _ = ReportsViewControl.RefreshActiveReportAsync();
            }
        }

        // ================= QUOTATIONS & PROJECTS =================

        private async System.Threading.Tasks.Task LoadProjectsAsync()
        {
            try
            {
                string searchKeyword = SearchProjectsBox?.Text?.Trim() ?? "";
                var projects = await QuotationRepository.GetProjectsAsync(searchKeyword);
                if (ProjectsGrid != null)
                {
                    ProjectsGrid.ItemsSource = projects;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchProjectsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = LoadProjectsAsync();
        }

        private async void CreateProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditProjectWindow
            {
                Owner = this
            };
            if (win.ShowDialog() == true)
            {
                await LoadProjectsAsync();
            }
        }

        private async void UploadQuotationButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new UploadQuotationWindow
            {
                Owner = this
            };
            if (win.ShowDialog() == true)
            {
                await LoadProjectsAsync();
            }
        }

        private async void ViewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int projectId)
            {
                var win = new ProjectQuotationsWindow(projectId)
                {
                    Owner = this
                };
                win.ShowDialog();
                await LoadProjectsAsync();
            }
        }

        private async void EditProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int projectId)
            {
                var projectToEdit = await QuotationRepository.GetProjectByIdAsync(projectId);
                if (projectToEdit != null)
                {
                    var win = new AddEditProjectWindow(projectToEdit)
                    {
                        Owner = this
                    };
                    if (win.ShowDialog() == true)
                    {
                        await LoadProjectsAsync();
                    }
                }
            }
        }

        private async void DeleteProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int projectId)
            {
                var res = MessageBox.Show("Are you sure you want to delete this project and all its vendor quotations?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    await QuotationRepository.DeleteProjectAsync(projectId);
                    await LoadProjectsAsync();
                }
            }
        }

        // ================= UTILITY REMINDERS =================

        private async System.Threading.Tasks.Task LoadUtilityRemindersAsync()
        {
            try
            {
                string searchKeyword = SearchRemindersBox?.Text?.Trim() ?? "";
                var reminders = await UtilityReminderRepository.GetRemindersAsync(searchKeyword);
                if (RemindersGrid != null)
                {
                    RemindersGrid.ItemsSource = reminders;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading utility reminders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchRemindersBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = LoadUtilityRemindersAsync();
        }

        private async void CreateReminderButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddEditReminderWindow
            {
                Owner = this
            };
            if (win.ShowDialog() == true)
            {
                await LoadUtilityRemindersAsync();
            }
        }

        private async void EditReminderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int reminderId)
            {
                var reminders = await UtilityReminderRepository.GetRemindersAsync();
                var reminderToEdit = reminders.FirstOrDefault(r => r.Id == reminderId);
                if (reminderToEdit != null)
                {
                    var win = new AddEditReminderWindow(reminderToEdit)
                    {
                        Owner = this
                    };
                    if (win.ShowDialog() == true)
                    {
                        await LoadUtilityRemindersAsync();
                    }
                }
            }
        }

        private async void DeleteReminderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int reminderId)
            {
                var res = MessageBox.Show("Are you sure you want to delete this utility reminder?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    await UtilityReminderRepository.DeleteReminderAsync(reminderId);
                    await LoadUtilityRemindersAsync();
                }
            }
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            _loginWindow.Show();
            Hide();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
