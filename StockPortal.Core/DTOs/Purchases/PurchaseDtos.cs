using System;
using System.Collections.Generic;

namespace StockPortal.Core.DTOs.Purchases
{
    public class PurchaseDto
    {
        public int Id { get; set; }
        public string PurchaseType { get; set; } = "Direct";
        public string? PoReference { get; set; }
        public string? InvoiceNumber { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? PaymentMode { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int? DistributionId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Remarks { get; set; }
        public decimal Total { get; set; }
        public string ReceiptBasis { get; set; } = "Invoice";
        public string? ChallanNumber { get; set; }
        public DateTime? ChallanDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceStatus { get; set; } = "Invoiced";
        public int? InvoiceGroupId { get; set; }
        public string? SerialNumbers { get; set; }
        public string? LinkedChallansDisplay { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    public class PurchaseItemDto
    {
        public int? Id { get; set; }
        public int PurchaseId { get; set; }
        public string? Category { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal GstPercent { get; set; }

        private decimal _subtotal;
        public decimal Subtotal
        {
            get => _subtotal != 0 ? _subtotal : Math.Round(Qty * Price, 2);
            set => _subtotal = value;
        }

        private decimal _gstAmount;
        public decimal GstAmount
        {
            get => _gstAmount != 0 ? _gstAmount : Math.Round(Subtotal * (GstPercent / 100m), 2);
            set => _gstAmount = value;
        }

        private decimal _total;
        public decimal Total
        {
            get => _total != 0 ? _total : Math.Round(Subtotal + GstAmount, 2);
            set => _total = value;
        }

        public decimal TotalAmount
        {
            get => Total;
            set => Total = value;
        }

        public string? SerialNumber { get; set; }
    }

    public class SavePurchaseRequestDto
    {
        public int? Id { get; set; }
        public string PurchaseType { get; set; } = "Direct";
        public string? PoReference { get; set; }
        public string? InvoiceNumber { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? PaymentMode { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string? Remarks { get; set; }
        public decimal Total { get; set; }
        public string ReceiptBasis { get; set; } = "Invoice";
        public string? ChallanNumber { get; set; }
        public DateTime? ChallanDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceStatus { get; set; } = "Invoiced";
        public int? InvoiceGroupId { get; set; }
        public string? SerialNumbers { get; set; }

        // Distribution (if Direct)
        public string? ShipmentId { get; set; }
        public string? Destination { get; set; }
        public DateTime? DistributionDate { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Position { get; set; }
        public string? DistributionRemark { get; set; }

        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    public class PendingChallanDto
    {
        public int Id { get; set; }
        public string ChallanNumber { get; set; } = string.Empty;
        public DateTime ChallanDate { get; set; }
        public decimal Total { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class LinkChallanRequestDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public int InvoiceGroupId { get; set; }
        public string? AppendRemark { get; set; }
    }

    public class CheckSerialNumbersRequestDto
    {
        public List<string> SerialNumbers { get; set; } = new();
        public int? CurrentPurchaseId { get; set; }
    }

    public class InvoiceFileDto
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[]? Data { get; set; }
        public string? UploadedAt { get; set; }
    }
}
