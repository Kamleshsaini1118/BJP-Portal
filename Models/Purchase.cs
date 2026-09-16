using System.Windows.Media;

namespace StockPortalApp.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public string PurchaseType { get; set; } = ""; // "Direct" or "In Store"
        public string? PoReference { get; set; }
        public string? InvoiceNumber { get; set; }
        public string VendorName { get; set; } = "";
        public string? PaymentMode { get; set; }
        public System.DateTime PurchaseDate { get; set; }
        public int? DistributionId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Remarks { get; set; }
        public decimal Total { get; set; }

        // ---- Challan support ----
        public string ReceiptBasis { get; set; } = "Invoice"; // "Invoice" or "Challan"
        public string? ChallanNumber { get; set; }
        public System.DateTime? ChallanDate { get; set; }
        public System.DateTime? InvoiceDate { get; set; }
        public string InvoiceStatus { get; set; } = "Invoiced"; // "Invoiced" or "Pending"
        public int? InvoiceGroupId { get; set; }

        /// <summary>Set by the caller after loading the list, since it depends on sibling rows sharing the same InvoiceGroupId.</summary>
        public string LinkedChallansDisplay { get; set; } = "";

        public bool IsChallanBasis => ReceiptBasis == "Challan";
        public bool IsPendingInvoice => InvoiceStatus == "Pending";
        public bool HasLinkedChallans => !string.IsNullOrWhiteSpace(LinkedChallansDisplay);

        public string PoReferenceDisplay => string.IsNullOrWhiteSpace(PoReference) ? "—" : PoReference;
        public string InvoiceNumberDisplay => string.IsNullOrWhiteSpace(InvoiceNumber) ? "—" : InvoiceNumber;
        public string PaymentModeDisplay => string.IsNullOrWhiteSpace(PaymentMode) ? "—" : PaymentMode;
        public string TotalDisplay => $"₹{Total:N2}";
        public string PurchaseDateDisplay => PurchaseDate.ToString("dd MMM yyyy");

        public string InvoiceOrChallanDisplay => IsChallanBasis
            ? $"Challan: {(string.IsNullOrWhiteSpace(ChallanNumber) ? "—" : ChallanNumber)}"
            : InvoiceNumberDisplay;

        public string ReceiptStatusDisplay => IsPendingInvoice ? "Challan Pending" : "Invoiced";

        public Brush ReceiptStatusBadgeBackground => IsPendingInvoice
            ? (Brush)new BrushConverter().ConvertFromString("#FFF0DE")!
            : (Brush)new BrushConverter().ConvertFromString("#E8F5E4")!;

        public Brush ReceiptStatusBadgeForeground => IsPendingInvoice
            ? (Brush)new BrushConverter().ConvertFromString("#E2600A")!
            : (Brush)new BrushConverter().ConvertFromString("#0C6606")!;
    }
}














// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace StockPortalApp.Models
// {
//     public class Purchase
//     {
//         public int Id { get; set; }
//         public string PurchaseType { get; set; } = ""; // "Direct" or "In Store"
//         public string? PoReference { get; set; }
//         public string? InvoiceNumber { get; set; }
//         public string VendorName { get; set; } = "";
//         public string? PaymentMode { get; set; }
//         public System.DateTime PurchaseDate { get; set; }
//         public int? DistributionId { get; set; }
//         public string? ReceiverName { get; set; }
//         public string? ReceiverNumber { get; set; }
//         public string? Remarks { get; set; }
//         public decimal Total { get; set; }

//         public string PoReferenceDisplay => string.IsNullOrWhiteSpace(PoReference) ? "—" : PoReference;
//         public string InvoiceNumberDisplay => string.IsNullOrWhiteSpace(InvoiceNumber) ? "—" : InvoiceNumber;
//         public string PaymentModeDisplay => string.IsNullOrWhiteSpace(PaymentMode) ? "—" : PaymentMode;
//         public string TotalDisplay => $"₹{Total:N2}";
//         public string PurchaseDateDisplay => PurchaseDate.ToString("dd MMM yyyy");
//     }
// }
