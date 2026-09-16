using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class FixedAssetSummary
    {
        public string Category { get; set; } = "";
        public string Item { get; set; } = "";
        public string Code { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Value { get; set; }

        public string QtyDisplay => Qty.ToString("N0");
    }
}
