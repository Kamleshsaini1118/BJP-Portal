using StockPortalApp.Data;
 using StockPortalApp.Helpers;
using StockPortalApp.Models;
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
    /// Interaction logic for DispatchWindow.xaml
    /// </summary>
    public partial class DispatchWindow : Window
    {
        private readonly FoodPacketBatch _batch;

        public ReceiptData? CreatedReceipt { get; private set; }

        public DispatchWindow(FoodPacketBatch batch)
        {
            InitializeComponent();
            _batch = batch;

            HeaderTitle.Text = $"Dispatch — {batch.BatchCode}";
            AvailableHintText.Text = $"Available: {batch.AvailableQty:N0} packets ({batch.ItemName})";
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var name = PersonNameBox.Text.Trim();
            var number = PersonNumberBox.Text.Trim();
            var qty = decimal.TryParse(QtyBox.Text.Trim(), out var q) ? q : 0;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(number))
            {
                MessageBox.Show("Enter the person's name and number before submitting.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!NumericInput.IsValidMobileNumber(number, isOptional: false))
            {
                MessageBox.Show("Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.", "Invalid Mobile Number",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PersonNumberBox.Focus();
                return;
            }
            if (qty <= 0)
            {
                MessageBox.Show("Enter a quantity greater than zero.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (qty > _batch.AvailableQty)
            {
                MessageBox.Show($"Only {_batch.AvailableQty:N0} packets are available for this batch. Enter a smaller quantity.",
                    "Too many", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SubmitButton.IsEnabled = false;
            try
            {
                await FoodPacketRepository.DispatchAsync(_batch.Id, name, number, qty, IndiaTime.Today);

                CreatedReceipt = new ReceiptData
                {
                    Type = "dispatched",
                    BatchId = _batch.BatchCode,
                    PersonName = name,
                    PersonNumber = number,
                    Items = new List<ReceiptItem> { new() { Name = _batch.ItemName, Qty = qty } },
                    Date = IndiaTime.Today.ToString("dd MMM yyyy")
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not record the dispatch.\n\n{ex.Message}", "Error",
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
