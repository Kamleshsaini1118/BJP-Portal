namespace StockPortalApp.Models
{
    public class GoodsReturnItem
    {
        public int Id { get; set; }
        public int GoodsReturnId { get; set; }
        public int? PurchaseItemId { get; set; }
        public string? Category { get; set; }
        public string ProductName { get; set; } = "";
        public string? SerialNumber { get; set; }
        public decimal ReturnQty { get; set; }
        public decimal Rate { get; set; }
        public decimal GstPercent { get; set; }
        public decimal Amount { get; set; }

        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "—" : Category;
        public string SerialNumberDisplay => string.IsNullOrWhiteSpace(SerialNumber) ? "—" : SerialNumber;
        public string ReturnQtyDisplay => ReturnQty % 1 == 0 ? ReturnQty.ToString("0") : ReturnQty.ToString("0.##");
        public string RateDisplay => $"₹{Rate:N2}";
        public string GstDisplay => $"{GstPercent:0.##}%";
        public string AmountDisplay => $"₹{Amount:N2}";
    }
}
