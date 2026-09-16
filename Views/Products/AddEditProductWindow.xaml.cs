using StockPortalApp.Data;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Interaction logic for AddEditProductWindow.xaml
    /// </summary>
    public partial class AddEditProductWindow : Window
    {
        private static readonly string[] Units = { "Packet", "Pcs", "Kg", "Box", "Litre", "Dozen", "MTR" };
        private static readonly ProductCategory AddNewSentinel = new() { Id = -1, Name = "+ Add New Category" };

        private readonly Product? _editingProduct;
        private List<ProductCategory> _categories = new();
        private bool _suppressSelectionHandler;

        /// <summary>The saved product, set only if the user clicked Submit successfully.</summary>
        public Product? SavedProduct { get; private set; }

        public AddEditProductWindow(List<ProductCategory> categories, Product? productToEdit = null)
        {
            InitializeComponent();
            _editingProduct = productToEdit;
            _categories = categories;

            UnitCombo.ItemsSource = Units;

            RebuildCategoryDropdown();

            if (_editingProduct != null)
            {
                Title = "Edit Product";
                HeaderTitle.Text = "Edit Product";
                SubmitButton.Content = "Save Changes";

                _suppressSelectionHandler = true;
                CategoryCombo.SelectedItem = _categories.FirstOrDefault(c => c.Id == _editingProduct.CategoryId);
                _suppressSelectionHandler = false;

                NameBox.Text = _editingProduct.Name;
                UnitCombo.SelectedItem = _editingProduct.Unit;
                // CodePreviewBox.Text = _editingProduct.ProductCode; // fixed, doesn't change on edit
                MinQtyBox.Text = _editingProduct.MinQty?.ToString(CultureInfo.InvariantCulture) ?? "";
                AvgPriceBox.Text = _editingProduct.AvgPrice?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }

        private void RebuildCategoryDropdown()
        {
            var items = new List<ProductCategory>(_categories) { AddNewSentinel };
            CategoryCombo.ItemsSource = items;
        }

        private void CategoryCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_suppressSelectionHandler) return;

            if (CategoryCombo.SelectedItem is ProductCategory cat && cat.Id == -1)
            {
                AddCategoryRow.Visibility = Visibility.Visible;
                NewCategoryNameBox.Focus();
                return;
            }

            AddCategoryRow.Visibility = Visibility.Collapsed;

            // Only refresh the "will be auto-generated" preview while adding a new product.
            // if (_editingProduct == null)
            // {
            //     CodePreviewBox.Text = "Auto-generated when you click Submit";
            // }
        }

        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NewCategoryNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                NewCategoryNameBox.Focus();
                return;
            }

            try
            {
                var existingPrefixes = _categories.Select(c => c.Prefix);
                var newCategory = await ProductRepository.AddCategoryAsync(name, existingPrefixes);

                _categories.Add(newCategory);
                RebuildCategoryDropdown();

                _suppressSelectionHandler = true;
                CategoryCombo.SelectedItem = _categories.First(c => c.Id == newCategory.Id);
                _suppressSelectionHandler = false;

                AddCategoryRow.Visibility = Visibility.Collapsed;
                NewCategoryNameBox.Text = "";

                // if (_editingProduct == null)
                // {
                //     CodePreviewBox.Text = "Auto-generated when you click Submit";
                // }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add the category.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryCombo.SelectedItem is not ProductCategory category || category.Id == -1)
            {
                MessageBox.Show("Select a product category before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter a product name before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UnitCombo.SelectedItem is not string unit)
            {
                MessageBox.Show("Select a product unit before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal? minQty = decimal.TryParse(MinQtyBox.Text.Trim(), out var mq) ? mq : null;
            decimal? avgPrice = decimal.TryParse(AvgPriceBox.Text.Trim(), out var ap) ? ap : null;

            var product = new Product
            {
                Id = _editingProduct?.Id,
                CategoryId = category.Id,
                Category = category.Name,
                Name = name,
                Unit = unit,
                MinQty = minQty,
                AvgPrice = avgPrice
            };

            SubmitButton.IsEnabled = false;
            try
            {
                var (id, code) = await ProductRepository.SaveProductAsync(product);
                product.Id = id;
                product.ProductCode = code;
                SavedProduct = product;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the product.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
