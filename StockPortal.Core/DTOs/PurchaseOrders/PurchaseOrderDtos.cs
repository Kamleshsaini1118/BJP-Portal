using System;
using System.Collections.Generic;

namespace StockPortal.Core.DTOs.PurchaseOrders
{
    public class PurchaseOrderDto
    {
        public int Id { get; set; }
        public string PoReference { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? Delivery { get; set; }
        public string ItemsList { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }

        // Aliases for backwards compatibility
        public DateTime PoDate { get => CreatedDate; set => CreatedDate = value; }
        public DateTime? ExpectedDate { get => Delivery; set => Delivery = value; }
        public decimal Total { get => TotalQty; set => TotalQty = value; }
        public string Status { get; set; } = "Active";
        public List<PurchaseOrderItemDisplayDto> Items { get; set; } = new();
    }

    public class PurchaseOrderItemDisplayDto
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public string? Category { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal EstimatedPrice { get => Price; set => Price = value; }
        public decimal GstPercent { get; set; }
        public decimal Subtotal => Qty * Price;
        public decimal GstAmount => Subtotal * (GstPercent / 100m);
        public decimal Total => Subtotal + GstAmount;
    }

    public class SavePurchaseOrderDto
    {
        public int? Id { get; set; }
        public string? PoReference { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public DateTime? Delivery { get; set; }
        public DateTime? ExpectedDate { get => Delivery; set => Delivery = value; }
        public string? Remarks { get; set; }
        public List<PurchaseOrderItemDisplayDto> Items { get; set; } = new();
    }
}
