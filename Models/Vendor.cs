using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class Vendor
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
        public string? Gst { get; set; }
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? BankHolder { get; set; }
        public string? BankAccount { get; set; }
        public string? BankIfsc { get; set; }
        public string? BankBranch { get; set; }
        public string CreatedAt { get; set; } = "";
    }
}
