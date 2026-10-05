using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class Product
    {
        public int? Id { get; set; }
        public string ProductCode { get; set; } = "";
        public string Name { get; set; } = "";
        public int CategoryId { get; set; }
        public string Category { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal? MinQty { get; set; }
        public decimal? AvgPrice { get; set; }

        public string ProductType { get; set; } = "";
        // Badge styling helpers for the DataGrid table
        public string ProductTypeBadgeBackground => ProductType switch
        {
            "Moving Asset" => "#DCFCE7", // Light green
            "Fixed Asset" => "#FFEDD5", // Light peach/orange
            _ => "#F3F4F6"
        };
        public string ProductTypeBadgeForeground => ProductType switch
        {
            "Moving Asset" => "#15803D", // Dark green
            "Fixed Asset" => "#C2410C", // Dark orange
            _ => "#4B5563"
        };

        public string CreatedAt { get; set; } = "";

        public bool IsActive { get; set; } = true;

        // Display helpers used directly by the DataGrid
        public string MinQtyDisplay => MinQty.HasValue ? MinQty.Value.ToString("N0") : "—";
        public string AvgPriceDisplay => AvgPrice.HasValue ? $"₹{AvgPrice.Value:N0}" : "—";

        public override string ToString() => Name;
    }
}
