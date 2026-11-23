using System;
using System.Collections.Generic;

namespace StockPortal.Core.DTOs.GoodsReturn
{
    public class GoodsReturnDto
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; } = string.Empty;
        public int PurchaseId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? AgainstPurchaseRef { get; set; }
        public string? InvoiceOrChallan { get => AgainstPurchaseRef; set => AgainstPurchaseRef = value; }
        public DateTime ReturnDate { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? PoReference { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalQty { get; set; }
        public string ItemsSummary { get; set; } = string.Empty;
        public string CategoriesList { get; set; } = string.Empty;
        public string ProductsList { get; set; } = string.Empty;
        public string? HandedOverTo { get; set; }
        public string? ContactNumber { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }

        // Vendor details (from sp_GetGoodsReturnDetails)
        public string? VendorGst { get; set; }
        public string? VendorAddress { get; set; }
        public string? VendorContactPerson { get; set; }
        public string? VendorContactNumber { get; set; }

        public List<GoodsReturnItemDto> Items { get; set; } = new();
    }

    public class GoodsReturnItemDto
    {
        public int Id { get; set; }
        public int GoodsReturnId { get; set; }
        public int? PurchaseItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal ReturnQty { get; set; }

        public decimal Rate { get; set; }
        public decimal UnitPrice { get => Rate; set => Rate = value; }

        public decimal GstPercent { get; set; }

        public decimal Amount { get; set; }
        public decimal TotalPrice { get => Amount; set => Amount = value; }

        public string? SerialNumber { get; set; }
    }

    public class ReturnablePurchaseOptionDto
    {
        public int PurchaseId { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
    }

    public class ReturnablePurchaseItemRowDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal PurchasedQty { get; set; }
        public decimal AlreadyReturnedQty { get; set; }
        public decimal MaxReturnableQty => Math.Max(0, PurchasedQty - AlreadyReturnedQty);
        public decimal UnitPrice { get; set; }
        public decimal GstPercent { get; set; }
        public string? SerialNumber { get; set; }
    }

    public class SaveGoodsReturnDto
    {
        public int PurchaseId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? InvoiceOrChallan { get; set; }
        public DateTime ReturnDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? Remarks { get; set; }
        public List<GoodsReturnItemDto> Items { get; set; } = new();
    }
}
