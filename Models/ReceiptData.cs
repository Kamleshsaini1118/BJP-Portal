using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class ReceiptItem
    {
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal GstPercent { get; set; }
        public decimal Total { get; set; }
    }

    public class ReceiptExtraField
    {
        public string Label { get; set; } = "";
        public string? Value { get; set; }
    }

    /// <summary>Type of receipt: "received", "dispatched", or "damage" — mirrors the HTML's openReceiptModal(type, data).</summary>
    public class ReceiptData
    {
        public string Type { get; set; } = "received";
        public string BatchId { get; set; } = "";
        public string IdLabel { get; set; } = "Batch ID";
        public string Subtitle { get; set; } = "Food Packets — Delivery Receipt";
        public string? PersonName { get; set; }
        public string? PersonNumber { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
        public string Date { get; set; } = "";
        public List<ReceiptExtraField> ExtraMeta { get; set; } = new();
    }
}
