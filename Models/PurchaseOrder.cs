using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string PoReference { get; set; } = "";
        public string VendorName { get; set; } = "";
        public System.DateTime CreatedDate { get; set; }
        public System.DateTime? Delivery { get; set; }
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
        public string PoReferenceDisplay => string.IsNullOrWhiteSpace(PoReference) ? "—" : PoReference;
        public string CreatedDateDisplay => CreatedDate.ToString("dd MMM yyyy");
        public string DeliveryDisplay => Delivery.HasValue ? Delivery.Value.ToString("dd MMM yyyy") : "—";
    }
}
