namespace StockPortalApp.Models
{
    /// <summary>One pending (not-yet-invoiced) challan-based purchase, shown as a checkable row in Combine Challans.</summary>
    public class PendingChallan
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = "";
        public System.DateTime? ChallanDate { get; set; }
        public System.DateTime PurchaseDate { get; set; }
        public string ItemsList { get; set; } = "";
        public decimal Total { get; set; }

        public string ChallanDateDisplay => ChallanDate.HasValue ? ChallanDate.Value.ToString("dd MMM yyyy") : "—";
        public string TotalDisplay => $"₹{Total:N2}";
    }
}