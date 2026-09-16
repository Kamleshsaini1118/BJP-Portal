using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class BatchItemRow : INotifyPropertyChanged
    {
        private string _name = "";
        private string _qty = "";
        private string _price = "";

        public string Name
        {
            get => _name;
            set { _name = value; OnChanged(nameof(Name)); }
        }

        public string Qty
        {
            get => _qty;
            set { _qty = value; OnChanged(nameof(Qty)); OnChanged(nameof(TotalDisplay)); }
        }

        public string Price
        {
            get => _price;
            set { _price = value; OnChanged(nameof(Price)); OnChanged(nameof(TotalDisplay)); }
        }

        public string TotalDisplay
        {
            get
            {
                var qty = decimal.TryParse(Qty, out var q) ? q : 0;
                var price = decimal.TryParse(Price, out var p) ? p : 0;
                return $"₹{qty * price:N0}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
