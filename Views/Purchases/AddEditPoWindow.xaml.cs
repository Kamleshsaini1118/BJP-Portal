using StockPortalApp.Data;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StockPortalApp
{
    /// <summary>
    /// Interaction logic for AddEditPoWindow.xaml
    /// </summary>
    public partial class AddEditPoWindow : Window
    {
        private readonly List<Product> _products;
        private readonly List<ProductCategory> _categories;
        private readonly PurchaseOrder? _editingPo;
        private readonly ObservableCollection<PurchaseOrderItemRow> _items = new();

        /// <summary>Bound by each row's Category ComboBox.</summary>
        public ObservableCollection<string> CategoryNames { get; } = new();

        /// <summary>Bound by each row's GST ComboBox.</summary>
        public List<decimal> GstOptions { get; } = new() { 0, 5, 12, 18, 28 };

        public AddEditPoWindow(
            List<string> vendorNames, List<ProductCategory> categories, List<Product> products,
            PurchaseOrder? poToEdit = null, List<PurchaseOrderItemRow>? existingItems = null)
        {
            InitializeComponent();
            DataContext = this;

            _products = products;
            _categories = categories;
            _editingPo = poToEdit;

            CategoryNames.Clear();
            foreach (var c in categories) CategoryNames.Add(c.Name);
            if (!CategoryNames.Contains("+ Add New Category"))
            {
                CategoryNames.Add("+ Add New Category");
            }

            var vList = new List<string>(vendorNames);
            if (!vList.Contains("+ Add New Vendor"))
            {
                vList.Add("+ Add New Vendor");
            }
            VendorCombo.ItemsSource = vList;
            ItemRowsControl.ItemsSource = _items;

            if (_editingPo != null)
            {
                Title = "Edit PO";
                HeaderTitle.Text = "Edit PO";
                SubmitButton.Content = "Save Changes";

                VendorCombo.SelectedItem = _editingPo.VendorName;
                DeliveryDatePicker.SelectedDate = _editingPo.Delivery;
                // PoRefPreviewBox.Text = _editingPo.PoReference;

                foreach (var item in existingItems ?? new List<PurchaseOrderItemRow>())
                {
                    var row = CreatePoItemRow();
                    row.Category = item.Category;
                    row.ProductName = item.ProductName;
                    row.Qty = item.Qty;
                    row.Price = item.Price;
                    row.GstPercent = item.GstPercent;
                    _items.Add(row);
                }
            }
            else
            {
                AddRow();
            }

            RecalculateGrandTotal();
        }

        private PurchaseOrderItemRow CreatePoItemRow()
        {
            var row = new PurchaseOrderItemRow(_products);
            row.OnAddNewCategoryRequested = (r) =>
            {
                OpenAddProductModal(r);
            };
            row.OnAddNewItemRequested = (r) =>
            {
                OpenAddProductModal(r);
            };
            row.PropertyChanged += (_, __) => RecalculateGrandTotal();
            return row;
        }

        private async void OpenAddProductModal(PurchaseOrderItemRow targetRow)
        {
            var dialog = new AddEditProductWindow(_categories) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.SavedProduct != null)
            {
                var p = dialog.SavedProduct;
                var freshCats = await ProductRepository.GetCategoriesAsync();
                var freshProds = await ProductRepository.GetProductsAsync();

                _categories.Clear();
                _categories.AddRange(freshCats);

                _products.Clear();
                _products.AddRange(freshProds);

                CategoryNames.Clear();
                foreach (var cat in freshCats)
                {
                    CategoryNames.Add(cat.Name);
                }
                if (!CategoryNames.Contains("+ Add New Category"))
                {
                    CategoryNames.Add("+ Add New Category");
                }

                foreach (var r in _items)
                {
                    r.UpdateProducts(_products);
                }

                targetRow.Category = p.Category;
                targetRow.ProductName = p.Name;
            }
        }

        private void AddRow() => _items.Add(CreatePoItemRow());

        private void AddRowButton_Click(object sender, RoutedEventArgs e) => AddRow();

        private void RecalculateGrandTotal()
        {
            var total = _items.Sum(r => r.TotalValue);
            GrandTotalText.Text = $"₹{total:N2}";
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e) => AddRow();

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: PurchaseOrderItemRow row }) return;
            _items.Remove(row);
            RecalculateGrandTotal();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var vendor = VendorCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(vendor))
            {
                MessageBox.Show("Select a vendor before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rowsData = _items
                .Where(r => !string.IsNullOrWhiteSpace(r.ProductName) && decimal.TryParse(r.Qty, out var q) && q > 0)
                .ToList();

            if (rowsData.Count == 0)
            {
                MessageBox.Show("Add at least one item with a product and quantity before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var delivery = DeliveryDatePicker.SelectedDate;
            var remarks = RemarksBox.Text.Trim();

            SubmitButton.IsEnabled = false;
            try
            {
                var (id, _) = await PurchaseOrderRepository.SaveHeaderAsync(_editingPo?.Id, vendor, delivery, remarks);

                await PurchaseOrderRepository.ClearItemsAsync(id);
                foreach (var row in rowsData)
                {
                    var qty = decimal.Parse(row.Qty);
                    var price = decimal.TryParse(row.Price, out var p) ? p : 0;
                    await PurchaseOrderRepository.AddItemAsync(id, row.Category, row.ProductName, qty, price, row.GstPercent);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the purchase order.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitButton.IsEnabled = true;
            }
        }

        private async void VendorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = VendorCombo.SelectedItem as string;
            if (selected == "+ Add New Vendor")
            {
                var dialog = new AddEditVendorWindow { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SavedVendor != null)
                {
                    var freshVendors = await VendorRepository.GetVendorsAsync();
                    var vList = freshVendors.Select(v => v.Name).ToList();
                    if (!vList.Contains("+ Add New Vendor"))
                    {
                        vList.Add("+ Add New Vendor");
                    }

                    VendorCombo.ItemsSource = vList;
                    VendorCombo.SelectedItem = dialog.SavedVendor.Name;
                }
                else
                {
                    VendorCombo.SelectedItem = null;
                }
            }
        }

        private async void NewProductCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var categories = await ProductRepository.GetCategoriesAsync();
            var dialog = new AddEditProductWindow(categories) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var freshCats = await ProductRepository.GetCategoriesAsync();
                var freshProds = await ProductRepository.GetProductsAsync();

                _products.Clear();
                _products.AddRange(freshProds);

                CategoryNames.Clear();
                foreach (var c in freshCats) CategoryNames.Add(c.Name);
                if (!CategoryNames.Contains("+ Add New Category")) CategoryNames.Add("+ Add New Category");

                foreach (var row in _items)
                {
                    row.UpdateProducts(_products);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
