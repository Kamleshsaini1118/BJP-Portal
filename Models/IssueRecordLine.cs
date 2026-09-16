using System.Windows.Media;

namespace StockPortalApp.Models
{
    /// <summary>One row in the Issue Goods list — one per item issued (the event/receiver info repeats per item, matching the design).</summary>
    public class IssueRecordLine
    {
        public int Id { get; set; } // IssueRecords.Id -- shared by every item row belonging to the same issue
        public string IssueCode { get; set; } = "";
        public string EventName { get; set; } = "";
        public string Item { get; set; } = "";
        public decimal Qty { get; set; }
        public string CreatedAt { get; set; } = "—";
        public System.DateTime? EventStartDate { get; set; }
        public System.DateTime DepositDate { get; set; }
        public string ReceiverName { get; set; } = "";
        public string ReceiverNumber { get; set; } = "";
        public string Status { get; set; } = ""; // "Pending" or "Deposited"

        public string QtyDisplay => Qty.ToString("N0");
        public string CreatedAtDisplay => string.IsNullOrWhiteSpace(CreatedAt) ? "—" : CreatedAt;
        public string DepositDateDisplay => DepositDate.ToString("dd MMM yyyy");
        public bool CanMarkDeposited => !string.Equals(Status?.Trim(), "Deposited", System.StringComparison.OrdinalIgnoreCase);

        public Brush StatusBadgeBackground => Status == "Deposited"
            ? (Brush)new BrushConverter().ConvertFromString("#E8F5E4")!
            : (Brush)new BrushConverter().ConvertFromString("#FFF0DE")!;

        public Brush StatusBadgeForeground => Status == "Deposited"
            ? (Brush)new BrushConverter().ConvertFromString("#0C6606")!
            : (Brush)new BrushConverter().ConvertFromString("#E2600A")!;
    }
}