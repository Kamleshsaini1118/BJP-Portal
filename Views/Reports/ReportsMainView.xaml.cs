using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace StockPortalApp.Views.Reports
{
    public partial class ReportsMainView : UserControl
    {
        public ReportsMainView()
        {
            InitializeComponent();
        }

        public async Task RefreshActiveReportAsync()
        {
            if (PurchaseReport.Visibility == Visibility.Visible)
                await PurchaseReport.LoadDataAsync();
            else if (DistributionReport.Visibility == Visibility.Visible)
                await DistributionReport.LoadDataAsync();
            else if (DamageReport.Visibility == Visibility.Visible)
                await DamageReport.LoadDataAsync();
            else if (VendorSummaryReport.Visibility == Visibility.Visible)
                await VendorSummaryReport.LoadDataAsync();
            else if (OpeningClosingReport.Visibility == Visibility.Visible)
                await OpeningClosingReport.LoadDataAsync();
            else if (StockValuationReport.Visibility == Visibility.Visible)
                await StockValuationReport.LoadDataAsync();
            else if (PendingChallansReport.Visibility == Visibility.Visible)
                await PendingChallansReport.LoadDataAsync();
            else if (QuotationComparisonReport.Visibility == Visibility.Visible)
                await QuotationComparisonReport.LoadDataAsync();
            else if (PoStatusReport.Visibility == Visibility.Visible)
                await PoStatusReport.LoadDataAsync();
            else if (IssueGoodsReport.Visibility == Visibility.Visible)
                await IssueGoodsReport.LoadDataAsync();
        }

        private async void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton toggle || toggle.Tag is not string tag) return;

            foreach (var other in new[]
            {
                TabPurchaseReport, TabDistributionReport, TabDamageReport, TabVendorSummary,
                TabOpeningClosing, TabStockValuation, TabPendingChallans, TabQuotationComparison,
                TabPoStatus, TabIssueGoods
            })
            {
                if (!ReferenceEquals(other, toggle)) other.IsChecked = false;
            }

            foreach (var report in new UserControl[]
            {
                PurchaseReport, DistributionReport, DamageReport, VendorSummaryReport,
                OpeningClosingReport, StockValuationReport, PendingChallansReport, QuotationComparisonReport,
                PoStatusReport, IssueGoodsReport
            })
            {
                report.Visibility = Visibility.Collapsed;
            }

            switch (tag)
            {
                case "purchase":
                    PurchaseReport.Visibility = Visibility.Visible;
                    await PurchaseReport.LoadDataAsync();
                    break;
                case "distribution":
                    DistributionReport.Visibility = Visibility.Visible;
                    await DistributionReport.LoadDataAsync();
                    break;
                case "damage":
                    DamageReport.Visibility = Visibility.Visible;
                    await DamageReport.LoadDataAsync();
                    break;
                case "vendor-summary":
                    VendorSummaryReport.Visibility = Visibility.Visible;
                    await VendorSummaryReport.LoadDataAsync();
                    break;
                case "opening-closing":
                    OpeningClosingReport.Visibility = Visibility.Visible;
                    await OpeningClosingReport.LoadDataAsync();
                    break;
                case "stock-valuation":
                    StockValuationReport.Visibility = Visibility.Visible;
                    await StockValuationReport.LoadDataAsync();
                    break;
                case "pending-challans":
                    PendingChallansReport.Visibility = Visibility.Visible;
                    await PendingChallansReport.LoadDataAsync();
                    break;
                case "quotation-comparison":
                    QuotationComparisonReport.Visibility = Visibility.Visible;
                    await QuotationComparisonReport.LoadDataAsync();
                    break;
                case "po-status":
                    PoStatusReport.Visibility = Visibility.Visible;
                    await PoStatusReport.LoadDataAsync();
                    break;
                case "issue-goods":
                    IssueGoodsReport.Visibility = Visibility.Visible;
                    await IssueGoodsReport.LoadDataAsync();
                    break;
            }
        }
    }
}
