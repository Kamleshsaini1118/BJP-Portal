using System;
using System.Windows.Media;

namespace StockPortalApp.Models
{
    public class VendorQuotation
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
        public decimal QuotationAmount { get; set; }
        public string Status { get; set; } = "Pending Review"; // Pending Review, Approved, Rejected
        public DateTime QuotationDate { get; set; } = DateTime.Today;
        public DateTime? ValidUntil { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsLowestQuote { get; set; } = false;

        // Display Helpers
        public string AmountDisplay => $"₹{QuotationAmount:N0}";
        public string QuotationDateDisplay => QuotationDate.ToString("dd MMM yyyy");
        public string ValidUntilDisplay => ValidUntil.HasValue ? ValidUntil.Value.ToString("dd MMM yyyy") : "—";
        public string SubtextDisplay => $"{QuotationNumber} · Dated {QuotationDateDisplay}" + (ValidUntil.HasValue ? $" · Valid till {ValidUntilDisplay}" : "");

        public Brush StatusBgBrush => Status switch
        {
            "Approved" => (Brush)new BrushConverter().ConvertFrom("#E8F5E9")!,
            "Rejected" => (Brush)new BrushConverter().ConvertFrom("#FFEBEE")!,
            _ => (Brush)new BrushConverter().ConvertFrom("#FFF3E0")!
        };

        public Brush StatusFgBrush => Status switch
        {
            "Approved" => (Brush)new BrushConverter().ConvertFrom("#2E7D32")!,
            "Rejected" => (Brush)new BrushConverter().ConvertFrom("#C62828")!,
            _ => (Brush)new BrushConverter().ConvertFrom("#E65100")!
        };
    }
}
