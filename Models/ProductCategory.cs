using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Prefix { get; set; } = "";

        public override string ToString() => Name; // so it displays nicely in a ComboBox
    }
}
