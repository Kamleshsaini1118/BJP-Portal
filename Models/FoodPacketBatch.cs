using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class FoodPacketBatch
    {
        public int Id { get; set; }
        public string BatchCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string? MealFor { get; set; }
        public string? VendorName { get; set; }
        public decimal Qty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal DispatchedQty { get; set; }
        public System.DateTime BatchDate { get; set; }
        public int DispatchCount { get; set; }

        // Display helpers for the DataGrid
        public string QtyDisplay => Qty.ToString("N0");
        public string DispatchedDisplay => DispatchedQty.ToString("N0");
        public string AvailableDisplay => AvailableQty.ToString("N0");
        public string VendorDisplay => string.IsNullOrWhiteSpace(VendorName) ? "—" : VendorName;
        public string DispatchButtonText => AvailableQty <= 0 ? "Dispatched" : "Dispatch";
        public bool CanDispatch => AvailableQty > 0;
        public string ViewDispatchesText => $"Dispatches ({DispatchCount})";
    }
}
