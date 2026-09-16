using System;

namespace StockPortalApp.Models
{
    public class PurchaseOrderItemDisplayRow
    {
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder OriginalPo { get; set; } = null!;
        public string PoReference { get; set; } = "";
        public string VendorName { get; set; } = "";
        public string? Category { get; set; }
        public string ItemName { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal GstPercent { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? Delivery { get; set; }

        public string PoReferenceDisplay => string.IsNullOrWhiteSpace(PoReference) ? "—" : PoReference;
        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "—" : Category;
        public string QtyDisplay => Qty.ToString("N0");
        public string UnitPriceDisplay => $"₹{UnitPrice:N2}";
        public string GstDisplay => $"{GstPercent:G29}%";
        public string LineTotalDisplay => $"₹{LineTotal:N2}";
        public string CreatedDateDisplay => CreatedDate.ToString("dd MMM yyyy");
        public string DeliveryDisplay => Delivery.HasValue ? Delivery.Value.ToString("dd MMM yyyy") : "—";
    }
}
