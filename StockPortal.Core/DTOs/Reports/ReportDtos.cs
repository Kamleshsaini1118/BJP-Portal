using System;

namespace StockPortal.Core.DTOs.Reports
{
    // public class StockReportItemDto
    // {
    //     public string ProductName { get; set; } = string.Empty;
    //     public string? Category { get; set; }
    //     public string? Unit { get; set; }
    //     public decimal OpeningStock { get; set; }
    //     public decimal TotalPurchased { get; set; }
    //     public decimal TotalIssuedOrDispatched { get; set; }
    //     public decimal TotalDamaged { get; set; }
    //     public decimal TotalReturned { get; set; }
    //     public decimal CurrentStock { get; set; }
    //     public decimal AvgPrice { get; set; }
    //     public decimal Valuation => CurrentStock * AvgPrice;
    // }

    public class StockReportItemDto
    {
        public string Item { get; set; } = string.Empty;
        public int Batches { get; set; }
        public decimal TotalQty { get; set; }
        public decimal Dispatched { get; set; }
        public decimal DamageQty { get; set; }
        public decimal IssueQty { get; set; }
        public decimal Remaining { get; set; }
    }

    // public class LedgerEntryDto
    // {
    //     public DateTime Date { get; set; }
    //     public string TransactionType { get; set; } = string.Empty; // "Purchase", "Dispatch", "Issue", "Damage", "Return"
    //     public string ReferenceNo { get; set; } = string.Empty;
    //     public string? EntityName { get; set; } // Vendor or Receiver or Room
    //     public decimal InQty { get; set; }
    //     public decimal OutQty { get; set; }
    //     public decimal Balance { get; set; }
    //     public string? Remarks { get; set; }
    // }

    public class LedgerEntryDto
    {
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string BatchCode { get => Code; set => Code = value; }
        public decimal Qty { get; set; }
        public string Party { get; set; } = string.Empty;
        public DateTime MoveDate { get; set; }
        public DateTime CreatedAt { get => MoveDate; set => MoveDate = value; }
    }

    public class PurchaseItemReportRowDto
    {
        public int PurchaseId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? InvoiceOrChallan { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal GstPercent { get; set; }
        public decimal Total { get; set; }
        public string? SerialNumber { get; set; }
    }
}
