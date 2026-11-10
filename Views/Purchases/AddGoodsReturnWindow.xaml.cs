using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp.Views.Purchases
{
    public partial class AddGoodsReturnWindow : Window
    {
        private readonly ObservableCollection<ReturnablePurchaseItemRow> _itemRows = new();
        private readonly HashSet<TextBox> _attachedQtyBoxes = new();
        private bool _isSaving;

        public static readonly IReadOnlyList<string> DefaultReasons = new[]
        {
            "Damaged",
            "Defective",
            "Expired",
            "Wrong Item",
            "Excess Quantity",
            "Quality Issue",
            "Other"
        };

        public int? SavedGoodsReturnId { get; private set; }

        public AddGoodsReturnWindow(List<string> vendorNames)
        {
            InitializeComponent();

            MaxHeight = SystemParameters.WorkArea.Height - 40;
            BodyScrollViewer.MaxHeight = SystemParameters.WorkArea.Height - 210;

            VendorCombo.ItemsSource = vendorNames
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v)
                .ToList();

            ReasonCombo.ItemsSource = DefaultReasons;
            ReturnDatePicker.SelectedDate = IndiaTime.Today;
            ReturnItemsControl.ItemsSource = _itemRows;

            NumericInputHelper.AttachDigitsOnly(ContactNumberBox);
        }

        private void ReturnQtyBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box && _attachedQtyBoxes.Add(box))
            {
                NumericInputHelper.AttachDecimalOnly(box);
            }
        }

        private async void VendorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PurchaseCombo.ItemsSource = null;
            PurchaseCombo.SelectedItem = null;
            PurchaseCombo.IsEnabled = false;
            ClearItemRows("Select a vendor and purchase to list its items.");

            if (VendorCombo.SelectedItem is not string vendorName || string.IsNullOrWhiteSpace(vendorName))
            {
                PurchaseCombo.Tag = "Select vendor first";
                return;
            }

            try
            {
                PurchaseCombo.Tag = "Loading purchases...";
                var purchases = await GoodsReturnRepository.GetVendorPurchasesForReturnAsync(vendorName);
                PurchaseCombo.ItemsSource = purchases;
                PurchaseCombo.IsEnabled = purchases.Count > 0;
                PurchaseCombo.Tag = purchases.Count > 0 ? "Select purchase" : "No purchases found for vendor";
            }
            catch (Exception ex)
            {
                PurchaseCombo.Tag = "Select vendor first";
                MessageBox.Show($"Could not load purchases for {vendorName}.\n\n{ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PurchaseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PurchaseCombo.SelectedItem is not ReturnablePurchaseOption purchase)
            {
                ClearItemRows("Select a vendor and purchase to list its items.");
                return;
            }

            try
            {
                ItemsEmptyText.Text = "Loading purchase items...";
                ItemsEmptyText.Visibility = Visibility.Visible;
                ReturnItemsControl.Visibility = Visibility.Collapsed;

                var items = await GoodsReturnRepository.GetPurchaseItemsForReturnAsync(purchase.Id);
                _itemRows.Clear();

                foreach (var item in items)
                {
                    item.OnQuantityChanged = RecalculateReturnTotal;
                    _itemRows.Add(item);
                }

                if (_itemRows.Count == 0)
                {
                    ItemsEmptyText.Text = "No items found on this purchase.";
                    ItemsEmptyText.Visibility = Visibility.Visible;
                    ReturnItemsControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ItemsEmptyText.Visibility = Visibility.Collapsed;
                    ReturnItemsControl.Visibility = Visibility.Visible;
                }

                RecalculateReturnTotal();
            }
            catch (Exception ex)
            {
                ClearItemRows("Could not load purchase items.");
                MessageBox.Show($"Could not load items for the selected purchase.\n\n{ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearItemRows(string message)
        {
            _itemRows.Clear();
            ItemsEmptyText.Text = message;
            ItemsEmptyText.Visibility = Visibility.Visible;
            ReturnItemsControl.Visibility = Visibility.Collapsed;
            RecalculateReturnTotal();
        }

        private void RecalculateReturnTotal()
        {
            decimal total = _itemRows.Sum(r => r.LineAmount);
            ReturnAmountText.Text = total == 0
                ? "₹0"
                : (total % 1 == 0 ? $"₹{total:N0}" : $"₹{total:N2}");
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;

            if (VendorCombo.SelectedItem is not string vendorName || string.IsNullOrWhiteSpace(vendorName))
            {
                MessageBox.Show("Please select a vendor.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                VendorCombo.Focus();
                return;
            }

            if (PurchaseCombo.SelectedItem is not ReturnablePurchaseOption selectedPurchase)
            {
                MessageBox.Show("Please select the purchase against which goods are being returned.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                PurchaseCombo.Focus();
                return;
            }

            if (ReturnDatePicker.SelectedDate is not DateTime returnDate)
            {
                MessageBox.Show("Please select a valid return date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReturnDatePicker.Focus();
                return;
            }

            if (ReasonCombo.SelectedItem is not string reason || string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Please select a return reason.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReasonCombo.Focus();
                return;
            }

            var overLimitItem = _itemRows.FirstOrDefault(r => r.IsOverLimit);
            if (overLimitItem != null)
            {
                MessageBox.Show(
                    $"Return quantity for \"{overLimitItem.ProductName}\" ({overLimitItem.ReturnQty:0.##}) cannot exceed Can Return ({overLimitItem.CanReturnDisplay}).",
                    "Invalid Return Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var itemsToReturn = _itemRows.Where(r => r.ReturnQty > 0).ToList();
            if (itemsToReturn.Count == 0)
            {
                MessageBox.Show("Please enter a return quantity greater than 0 for at least one item.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string contact = ContactNumberBox.Text.Trim();
            if (!string.IsNullOrEmpty(contact) && contact.Length < 10)
            {
                MessageBox.Show("Please enter a valid 10-digit contact number or leave it blank.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                ContactNumberBox.Focus();
                return;
            }

            decimal totalAmount = itemsToReturn.Sum(r => r.LineAmount);

            _isSaving = true;
            SaveButton.IsEnabled = false;
            SaveButton.Content = "Saving...";

            try
            {
                var (id, returnNo) = await GoodsReturnRepository.SaveGoodsReturnAsync(
                    returnDate,
                    vendorName,
                    selectedPurchase.Id,
                    selectedPurchase.ReferenceCode,
                    reason,
                    totalAmount,
                    HandedOverToBox.Text,
                    contact,
                    RemarksBox.Text,
                    itemsToReturn);

                SavedGoodsReturnId = id;
                MessageBox.Show($"Goods return {returnNo} saved successfully.", "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save goods return.\n\n{ex.Message}",
                    "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isSaving = false;
                SaveButton.IsEnabled = true;
                SaveButton.Content = "Save Return";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
