using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace StockPortalApp
{
    public partial class SelectProductExportDialog : Window
    {
        public string? SelectedProduct { get; private set; }

        public SelectProductExportDialog(IEnumerable<string> products, string? defaultProduct = null)
        {
            InitializeComponent();

            var productList = products.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().OrderBy(p => p).ToList();
            ProductCombo.ItemsSource = productList;

            if (!string.IsNullOrEmpty(defaultProduct) && productList.Contains(defaultProduct))
            {
                ProductCombo.SelectedItem = defaultProduct;
            }
            else if (productList.Count > 0)
            {
                ProductCombo.SelectedIndex = 0;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductCombo.SelectedItem is not string selectedProduct || string.IsNullOrWhiteSpace(selectedProduct))
            {
                MessageBox.Show("Please select a product from the list.", "Product Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedProduct = selectedProduct;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
