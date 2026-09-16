using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    /// <summary>An item row for the "Distribution Details" section (category + item + qty only, no price/GST).</summary>
    public class DistributionItemRow : INotifyPropertyChanged
    {
        private readonly List<Product> _allProducts;
        private string? _category;
        private string _productName = "";
        private string _qty = "";

        public DistributionItemRow(List<Product> allProducts)
        {
            _allProducts = allProducts;
            RecalculateAvailableItems();
        }

        public ObservableCollection<string> AvailableItems { get; } = new();

        public Action<DistributionItemRow>? OnAddNewCategoryRequested { get; set; }
        public Action<DistributionItemRow>? OnAddNewItemRequested { get; set; }

        public string? Category
        {
            get => _category;
            set
            {
                _category = value;
                OnChanged(nameof(Category));
                RecalculateAvailableItems();
                if (value == "+ Add New Category")
                {
                    OnAddNewCategoryRequested?.Invoke(this);
                }
            }
        }

        public void UpdateProducts(List<Product> newProducts)
        {
            _allProducts.Clear();
            _allProducts.AddRange(newProducts);
            RecalculateAvailableItems();
        }

        private void RecalculateAvailableItems()
        {
            AvailableItems.Clear();
            var items = string.IsNullOrEmpty(Category) || Category == "+ Add New Category"
                ? _allProducts.Select(p => p.Name)
                : _allProducts.Where(p => p.Category == Category).Select(p => p.Name);
            foreach (var name in items.Distinct()) AvailableItems.Add(name);
            if (!AvailableItems.Contains("+ Add New Item"))
            {
                AvailableItems.Add("+ Add New Item");
            }
        }

        public string ProductName
        {
            get => _productName;
            set
            {
                _productName = value;
                OnChanged(nameof(ProductName));
                if (value == "+ Add New Item")
                {
                    OnAddNewItemRequested?.Invoke(this);
                }
            }
        }

        public string Qty
        {
            get => _qty;
            set { _qty = value; OnChanged(nameof(Qty)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
