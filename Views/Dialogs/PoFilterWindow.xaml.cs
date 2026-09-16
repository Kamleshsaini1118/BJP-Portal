using System;
using System.Collections.Generic;
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
    /// Interaction logic for PoFilterWindow.xaml
    /// </summary>
    public partial class PoFilterWindow : Window
    {
        public string? SelectedVendor { get; private set; }
        public string? SelectedProduct { get; private set; }
        public DateTime? SelectedDateFrom { get; private set; }
        public DateTime? SelectedDateTo { get; private set; }

        public PoFilterWindow(
            List<string> vendorNames, List<string> productNames,
            string? currentVendor, string? currentProduct, DateTime? currentFrom, DateTime? currentTo)
        {
            InitializeComponent();

            var vendorItems = new List<string> { "Any vendor" };
            vendorItems.AddRange(vendorNames);
            VendorCombo.ItemsSource = vendorItems;
            VendorCombo.SelectedItem = currentVendor ?? "Any vendor";

            var productItems = new List<string> { "Any product" };
            productItems.AddRange(productNames);
            ProductCombo.ItemsSource = productItems;
            ProductCombo.SelectedItem = currentProduct ?? "Any product";

            DateFromPicker.SelectedDate = currentFrom;
            DateToPicker.SelectedDate = currentTo;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var vendor = VendorCombo.SelectedItem as string;
            var product = ProductCombo.SelectedItem as string;

            SelectedVendor = (vendor == "Any vendor") ? null : vendor;
            SelectedProduct = (product == "Any product") ? null : product;
            SelectedDateFrom = DateFromPicker.SelectedDate;
            SelectedDateTo = DateToPicker.SelectedDate;

            DialogResult = true;
            Close();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedVendor = null;
            SelectedProduct = null;
            SelectedDateFrom = null;
            SelectedDateTo = null;

            DialogResult = true;
            Close();
        }
    }
}
