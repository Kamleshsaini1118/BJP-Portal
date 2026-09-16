using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class AssetDistributionDetail
    {
        public string Destination { get; set; } = "";
        public decimal Qty { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Position { get; set; }
        public System.DateTime DistributionDate { get; set; }

        public string QtyDisplay => Qty.ToString("N0");
        public string ReceiverNameDisplay => string.IsNullOrWhiteSpace(ReceiverName) ? "—" : ReceiverName;
        public string ReceiverNumberDisplay => string.IsNullOrWhiteSpace(ReceiverNumber) ? "—" : ReceiverNumber;
        public string PositionDisplay => string.IsNullOrWhiteSpace(Position) ? "—" : Position;
        public string DateDisplay => DistributionDate.ToString("dd MMM yyyy");
    }
}
