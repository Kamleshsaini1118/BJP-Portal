using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class BatchDispatchRecord
    {
        public int Id { get; set; }
        public string PersonName { get; set; } = "";
        public string PersonNumber { get; set; } = "";
        public decimal Qty { get; set; }
        public System.DateTime DispatchDate { get; set; }
        public System.DateTime CreatedAt { get; set; }

        public string QtyDisplay => Qty.ToString("N0");
        public string TimeDisplay => CreatedAt.ToString("dd MMM, hh:mm tt");
    }
}
