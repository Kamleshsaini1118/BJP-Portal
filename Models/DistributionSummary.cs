using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class DistributionSummary
    {
        public int Id { get; set; }
        public string ShipmentId { get; set; } = "";
        public string Destination { get; set; } = "";
        public System.DateTime DistributionDate { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Position { get; set; }
        public string? Remark { get; set; }
        public string ItemsList { get; set; } = ""; // comma-separated product names from the SP
        public decimal TotalQty { get; set; }

        public string ItemsSummaryDisplay
        {
            get
            {
                var names = ItemsList.Split(", ", System.StringSplitOptions.RemoveEmptyEntries);
                if (names.Length <= 1) return ItemsList;
                return $"{names[0]} +{names.Length - 1} more";
            }
        }

        public string TotalQtyDisplay => TotalQty.ToString("N0");
        public string DateDisplay => DistributionDate.ToString("dd MMM yyyy");
    }
}
