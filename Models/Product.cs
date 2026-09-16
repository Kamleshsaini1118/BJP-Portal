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

        public string CreatedAt { get; set;} = "";

        // Display helpers used directly by the DataGrid
        public string MinQtyDisplay => MinQty.HasValue ? MinQty.Value.ToString("N0") : "—";
        public string AvgPriceDisplay => AvgPrice.HasValue ? $"₹{AvgPrice.Value:N0}" : "—";
    }
}
