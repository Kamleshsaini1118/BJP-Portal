using System;
using System.Windows.Media;

namespace StockPortalApp.Models
{
    public class QuotationProject
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ClientDepartment { get; set; } = string.Empty;
        public string Category { get; set; } = "Select Category";
        public int QuotationCount { get; set; } = 0;
        public decimal LowestQuote { get; set; } = 0m;
        public string Status { get; set; } = "Open";
        public DateTime DateCreated { get; set; } = DateTime.Today;
        public decimal? EstimatedBudget { get; set; }
        public string Remarks { get; set; } = string.Empty;

        // Display Helpers
        public string LowestQuoteDisplay => LowestQuote > 0 ? $"₹{LowestQuote:N0}" : "—";
        public string DateCreatedDisplay => DateCreated.ToString("dd MMM yyyy");
        public string EstimatedBudgetDisplay => EstimatedBudget.HasValue && EstimatedBudget.Value > 0 ? $"₹{EstimatedBudget.Value:N0}" : "—";

        public Brush StatusBgBrush => Status switch
        {
            "Open" => (Brush)new BrushConverter().ConvertFrom("#FFF3E0")!,
            "Finalized" => (Brush)new BrushConverter().ConvertFrom("#E8F5E9")!,
            "Cancelled" => (Brush)new BrushConverter().ConvertFrom("#FFEBEE")!,
            _ => (Brush)new BrushConverter().ConvertFrom("#F5F5F5")!
        };

        public Brush StatusFgBrush => Status switch
        {
            "Open" => (Brush)new BrushConverter().ConvertFrom("#E65100")!,
            "Finalized" => (Brush)new BrushConverter().ConvertFrom("#2E7D32")!,
            "Cancelled" => (Brush)new BrushConverter().ConvertFrom("#C62828")!,
            _ => (Brush)new BrushConverter().ConvertFrom("#616161")!
        };
    }
}
