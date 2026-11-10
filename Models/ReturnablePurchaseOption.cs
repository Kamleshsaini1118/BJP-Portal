using System;

namespace StockPortalApp.Models
{
    public class ReturnablePurchaseOption
    {
        public int Id { get; set; }
        public string PurchaseType { get; set; } = "";
        public string ReceiptBasis { get; set; } = "Invoice";
        public string? InvoiceNumber { get; set; }
        public string? ChallanNumber { get; set; }
        public string? PoReference { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal Total { get; set; }

        public string ReferenceCode
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(InvoiceNumber)) return InvoiceNumber.Trim();
                if (!string.IsNullOrWhiteSpace(ChallanNumber)) return $"Challan: {ChallanNumber.Trim()}";
                if (!string.IsNullOrWhiteSpace(PoReference)) return PoReference.Trim();
                return $"#{Id}";
            }
        }

        public string DisplayLabel => $"{ReferenceCode} ({PurchaseDate:dd MMM yyyy}) — ₹{Total:N2}";

        public override string ToString() => DisplayLabel;
    }
}
