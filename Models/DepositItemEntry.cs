namespace StockPortalApp.Models
{
    /// <summary>One item row in the Mark Items Deposited popup.</summary>
    public class DepositItemEntry
    {
        public int Id { get; set; } // IssueRecordItems.Id
        public string ProductName { get; set; } = "";
        public decimal IssuedQty { get; set; }

        public string IssuedQtyDisplay => IssuedQty.ToString("N0");
    }
}