using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class StockReportItem
    {
        public string Item { get; set; } = "";
        public int Batches { get; set; }
        public decimal TotalQty { get; set; }
        public decimal Dispatched { get; set; }
        public decimal DamageQty { get; set; }
        public decimal IssueQty { get; set; }
        public decimal Remaining { get; set; }

        public string TotalQtyDisplay => TotalQty.ToString("N0");
        public string DispatchedDisplay => Dispatched.ToString("N0");
        public string DamageQtyDisplay => DamageQty.ToString("N0");
        public string IssueQtyDisplay => IssueQty.ToString("N0");
        public string RemainingDisplay => Remaining.ToString("N0");
    }
}
