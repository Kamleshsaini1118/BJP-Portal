using System;

namespace StockPortalApp.Models
{
    public class PurchaseItemReportRow
    {
        public int ItemId { get; set; }
        public int PurchaseId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string VendorName { get; set; } = "";
        public string? PoReference { get; set; }
        public string? PaymentMode { get; set; }

        public string? Category { get; set; }
        public string ProductName { get; set; } = "";
        public string? SerialNumber { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal GstPercent { get; set; }

        public decimal Subtotal => Qty * Price;
        public decimal GstAmount => Subtotal * (GstPercent / 100m);
        public decimal TotalAmount => Subtotal + GstAmount;

        public string PurchaseDateDisplay => PurchaseDate.ToString("dd MMM yyyy");
        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "—" : Category;
        public string SerialNumberDisplay => string.IsNullOrWhiteSpace(SerialNumber) ? "—" : SerialNumber;
        public string QtyDisplay => Qty.ToString("N0");
        public string PriceDisplay => $"₹{Price:N2}";
        public string GstPercentDisplay => $"{GstPercent:G29}%";
        public string GstAmountDisplay => $"₹{GstAmount:N2}";
        public string TotalAmountDisplay => $"₹{TotalAmount:N2}";
    }
}
