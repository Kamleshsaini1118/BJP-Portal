using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Interaction logic for NewBatchWindow.xaml
    /// </summary>
    public partial class NewBatchWindow : Window
    {
        private readonly ObservableCollection<BatchItemRow> _items = new();

        /// <summary>Set when Submit succeeds, so the caller can show a receipt for what was created.</summary>
        public ReceiptData? CreatedReceipt { get; private set; }

        public NewBatchWindow(System.Collections.Generic.List<string> vendorNames)
        {
            InitializeComponent();

            MealForCombo.ItemsSource = new[] { "Breakfast", "Lunch", "Dinner" };
            BatchDatePicker.SelectedDate = IndiaTime.Today;

            var vendorItems = new System.Collections.Generic.List<string>(vendorNames) { "+ Add New Vendor" };
            VendorCombo.ItemsSource = vendorItems;

            ItemRowsControl.ItemsSource = _items;
            AddRow();
        }

        private void AddRow()
        {
            var row = new BatchItemRow();
            row.PropertyChanged += Row_PropertyChanged;
            _items.Add(row);
            RecalculateGrandTotal();
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateGrandTotal();

        private void RecalculateGrandTotal()
        {
            decimal total = 0;
            foreach (var row in _items)
            {
                var qty = decimal.TryParse(row.Qty, out var q) ? q : 0;
                var price = decimal.TryParse(row.Price, out var p) ? p : 0;
                total += qty * price;
            }
            GrandTotalText.Text = $"₹{total:N0}";
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e) => AddRow();

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: BatchItemRow row }) return;
            row.PropertyChanged -= Row_PropertyChanged;
            _items.Remove(row);
            RecalculateGrandTotal();
        }

        private async void VendorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VendorCombo.SelectedItem as string == "+ Add New Vendor")
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

        // private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        // {
        //     var rowsData = _items
        //         .Select(r => new { Name = r.Name.Trim(), Qty = decimal.TryParse(r.Qty, out var q) ? q : 0 })
        //         .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Qty > 0)
        //         .ToList();

        //     if (rowsData.Count == 0)
        //     {
        //         MessageBox.Show("Add at least one item with a name and quantity before submitting.", "Missing information",
        //             MessageBoxButton.OK, MessageBoxImage.Warning);
        //         return;
        //     }

        //     var mealFor = MealForCombo.SelectedItem as string;
        //     var vendorName = VendorCombo.SelectedItem as string;
        //     if (vendorName == "+ Add New Vendor") vendorName = null;
        //     var batchDate = BatchDatePicker.SelectedDate ?? IndiaTime.Today;
        //     var receiverName = ReceiverNameBox.Text.Trim();
        //     var receiverNumber = ReceiverNumberBox.Text.Trim();

        //     SubmitButton.IsEnabled = false;
        //     try
        //     {
        //         string? firstBatchCode = null;
        //         foreach (var item in rowsData)
        //         {
        //             var (_, code) = await FoodPacketRepository.AddBatchAsync(
        //                 item.Name, mealFor, vendorName, item.Qty, batchDate, receiverName, receiverNumber);
        //             firstBatchCode ??= code;
        //         }

        //         CreatedReceipt = new ReceiptData
        //         {
        //             Type = "received",
        //             BatchId = firstBatchCode ?? "",
        //             PersonName = receiverName,
        //             PersonNumber = receiverNumber,
        //             Items = rowsData.Select(r => new ReceiptItem { Name = r.Name, Qty = r.Qty }).ToList(),
        //             Date = IndiaTime.Today.ToString("dd MMM yyyy")
        //         };

        //         DialogResult = true;
        //         Close();
        //     }
        //     catch (Exception ex)
        //     {
        //         MessageBox.Show($"Could not save the batch.\n\n{ex.Message}", "Error",
        //             MessageBoxButton.OK, MessageBoxImage.Error);
        //         SubmitButton.IsEnabled = true;
        //     }
        // }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate item rows
            foreach (var row in _items)
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    MessageBox.Show(
                        "Item name cannot be empty.",
                        "Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Validate Quantity
                if (!decimal.TryParse(row.Qty, out var qty) || qty <= 0)
                {
                    MessageBox.Show(
                        $"Please enter a valid quantity greater than 0 for '{row.Name}'.",
                        "Invalid Quantity",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // Validate Price
                if (!decimal.TryParse(row.Price, out var price) || price < 0)
                {
                    MessageBox.Show(
                        $"Please enter a valid price for '{row.Name}'.",
                        "Invalid Price",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }

            // Validate Receiver Name
            var receiverName = ReceiverNameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(receiverName))
            {
                MessageBox.Show(
                    "Please enter the receiver name.",
                    "Missing Receiver Name",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                ReceiverNameBox.Focus();
                return;
            }

            // Validate Receiver Number
            var receiverNumber = ReceiverNumberBox.Text.Trim();

            if (!NumericInput.IsValidMobileNumber(receiverNumber, isOptional: false))
            {
                MessageBox.Show(
                    "Please enter a valid 10-digit receiver mobile number starting with 6, 7, 8, or 9.",
                    "Invalid Receiver Number",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                ReceiverNumberBox.Focus();
                return;
            }

            // Prepare valid rows
            var rowsData = _items
                .Select(r => new
                {
                    Name = r.Name.Trim(),
                    Qty = decimal.Parse(r.Qty),
                    Price = decimal.Parse(r.Price)
                })
                .ToList();

            var mealFor = MealForCombo.SelectedItem as string;

            var vendorName = VendorCombo.SelectedItem as string;

            if (vendorName == "+ Add New Vendor")
                vendorName = null;

            var batchDate = BatchDatePicker.SelectedDate ?? IndiaTime.Today;

            SubmitButton.IsEnabled = false;

            try
            {
                string? firstBatchCode = null;

                foreach (var item in rowsData)
                {
                    var (_, code) = await FoodPacketRepository.AddBatchAsync(
                        item.Name,
                        mealFor,
                        vendorName,
                        item.Qty,
                        batchDate,
                        receiverName,
                        receiverNumber
                    );

                    firstBatchCode ??= code;
                }

                CreatedReceipt = new ReceiptData
                {
                    Type = "received",
                    BatchId = firstBatchCode ?? "",
                    PersonName = receiverName,
                    PersonNumber = receiverNumber,

                    Items = rowsData
                        .Select(r => new ReceiptItem
                        {
                            Name = r.Name,
                            Qty = r.Qty
                        })
                        .ToList(),

                    Date = IndiaTime.Today.ToString("dd MMM yyyy")
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save the batch.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

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
