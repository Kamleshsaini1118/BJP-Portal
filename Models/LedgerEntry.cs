using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StockPortalApp.Models
{
    public class LedgerEntry
    {
        public string Type { get; set; } = ""; // "Batch Added" or "Dispatched"
        public string BatchCode { get; set; } = "";
        public string Item { get; set; } = "";
        public decimal Qty { get; set; }
        public string Party { get; set; } = "";
        public System.DateTime CreatedAt { get; set; }

        public string QtyDisplay => Qty.ToString("N0");
        public string CreatedAtDisplay => (CreatedAt == DateTime.MinValue || CreatedAt.Year <= 1900)
            ? "—"
            : (CreatedAt.TimeOfDay == TimeSpan.Zero
                ? CreatedAt.ToString("dd MMM yyyy")
                : CreatedAt.ToString("dd MMM yyyy, hh:mm tt"));
        public string TimeDisplay => CreatedAtDisplay;

        // Badge colors: green for "Batch Added" (.tag.ok), saffron for "Dispatched" (.tag.pending)
        // public Brush TypeBadgeBackground => Type == "Batch Added"
        //     ? (Brush)new BrushConverter().ConvertFromString("#E8F5E4")!
        //     : (Brush)new BrushConverter().ConvertFromString("#FFF0DE")!;

        // public Brush TypeBadgeForeground => Type == "Batch Added"
        //     ? (Brush)new BrushConverter().ConvertFromString("#0C6606")!
        //     : (Brush)new BrushConverter().ConvertFromString("#E2600A")!;


        // Badge colors: green for stock coming IN, saffron for going OUT, red for damage
        public Brush TypeBadgeBackground => Type switch
        {
            "Batch Added" or "Purchase Added" or "Deposited" => (Brush)new BrushConverter().ConvertFromString("#E8F5E4")!,
            "Damaged" => (Brush)new BrushConverter().ConvertFromString("#FDE8E4")!,
            _ => (Brush)new BrushConverter().ConvertFromString("#FFF0DE")!,
        };

        public Brush TypeBadgeForeground => Type switch
        {
            "Batch Added" or "Purchase Added" or "Deposited" => (Brush)new BrushConverter().ConvertFromString("#0C6606")!,
            "Damaged" => (Brush)new BrushConverter().ConvertFromString("#B03A2E")!,
            _ => (Brush)new BrushConverter().ConvertFromString("#E2600A")!,
        };
    }
}
