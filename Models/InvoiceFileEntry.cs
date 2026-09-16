using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    /// <summary>One invoice file attached to a purchase. Id is null for a newly picked file not yet saved.</summary>
    public class InvoiceFileEntry
    {
        public int? Id { get; set; }
        public string FileName { get; set; } = "";
        public byte[]? Data { get; set; } // only set for newly-picked, not-yet-saved files
    }
}
