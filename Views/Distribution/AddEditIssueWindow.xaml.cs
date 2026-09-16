using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StockPortalApp.Data;
 using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp
{
    public partial class AddEditIssueWindow : Window
    {
        private readonly List<Product> _products;
        private readonly List<ProductCategory> _categories;
        private readonly IssueRecordHeader? _editingHeader;
        private readonly ObservableCollection<DistributionItemRow> _items = new();

        public ObservableCollection<string> CategoryNames { get; } = new();

        public AddEditIssueWindow(
            List<ProductCategory> categories, List<Product> products,
            IssueRecordHeader? headerToEdit = null, List<DistributionItemRow>? existingItems = null)
        {
            InitializeComponent();
            DataContext = this;

            MaxHeight = SystemParameters.WorkArea.Height - 40;
            BodyScrollViewer.MaxHeight = SystemParameters.WorkArea.Height - 220;

            _products = products;
            _categories = categories;
            _editingHeader = headerToEdit;

            CategoryNames.Clear();
            foreach (var c in categories) CategoryNames.Add(c.Name);
            if (!CategoryNames.Contains("+ Add New Category"))
            {
                CategoryNames.Add("+ Add New Category");
            }

            ItemRowsControl.ItemsSource = _items;
            DepositDatePicker.SelectedDate = IndiaTime.Today.AddDays(0);

            if (_editingHeader != null)
            {
                Title = "Edit Issue";
                HeaderTitle.Text = "Edit Issue";
                SubmitButton.Content = "Save Changes";

                EventNameBox.Text = _editingHeader.EventName;
                EventStartDatePicker.SelectedDate = _editingHeader.EventStartDate;
                EventEndDatePicker.SelectedDate = _editingHeader.EventEndDate;
                DepositDatePicker.SelectedDate = _editingHeader.DepositDate;
                VenueAddressBox.Text = _editingHeader.VenueAddress;
                ReceiverNameBox.Text = _editingHeader.ReceiverName;
                ReceiverNumberBox.Text = _editingHeader.ReceiverNumber;
                ReceiverPositionBox.Text = _editingHeader.ReceiverPosition;
                RemarkBox.Text = _editingHeader.Remark;

                foreach (var item in existingItems ?? new List<DistributionItemRow>())
                {
                    var row = CreateItemRow();
                    row.Category = item.Category;
                    row.ProductName = item.ProductName;
                    row.Qty = item.Qty;
                    _items.Add(row);
                }

                if (_items.Count == 0) AddItemRow();
            }
            else
            {
                AddItemRow();
            }
        }

        private DistributionItemRow CreateItemRow()
        {
            var row = new DistributionItemRow(_products);
            row.OnAddNewCategoryRequested = (r) =>
            {
                OpenAddProductModal(r);
            };
            row.OnAddNewItemRequested = (r) =>
            {
                OpenAddProductModal(r);
            };
            return row;
        }

        private async void OpenAddProductModal(DistributionItemRow targetRow)
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

        private void AddItemRow() => _items.Add(CreateItemRow());

        private void AddItemButton_Click(object sender, RoutedEventArgs e) => AddItemRow();

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DistributionItemRow row }) return;
            _items.Remove(row);
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var eventName = EventNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(eventName))
            {
                MessageBox.Show("Enter an event name before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var receiverName = ReceiverNameBox.Text.Trim();
            var receiverNumber = ReceiverNumberBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(receiverNumber))
            {
                MessageBox.Show("Enter the receiver's name and number before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!NumericInput.IsValidMobileNumber(receiverNumber, isOptional: false))
            {
                MessageBox.Show("Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.", "Invalid Mobile Number",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReceiverNumberBox.Focus();
                return;
            }

            var depositDate = DepositDatePicker.SelectedDate;
            if (!depositDate.HasValue)
            {
                MessageBox.Show("Pick a deposit date before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rows = _items
                .Where(r => !string.IsNullOrWhiteSpace(r.ProductName) && decimal.TryParse(r.Qty, out var q) && q > 0)
                .ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show("Add at least one item with a quantity before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SubmitButton.IsEnabled = false;
            try
            {
                var venueAddress = VenueAddressBox.Text.Trim();
                var receiverPosition = ReceiverPositionBox.Text.Trim();
                var remark = RemarkBox.Text.Trim();

                var (id, _) = await IssueRepository.SaveHeaderAsync(
                    _editingHeader?.Id, eventName,
                    EventStartDatePicker.SelectedDate, EventEndDatePicker.SelectedDate, depositDate.Value,
                    string.IsNullOrWhiteSpace(venueAddress) ? null : venueAddress,
                    receiverName, receiverNumber,
                    string.IsNullOrWhiteSpace(receiverPosition) ? null : receiverPosition,
                    string.IsNullOrWhiteSpace(remark) ? null : remark);

                await IssueRepository.ClearItemsAsync(id);
                foreach (var row in rows)
                {
                    var qty = decimal.Parse(row.Qty);
                    await IssueRepository.AddItemAsync(id, row.Category, row.ProductName, qty);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the issue.\n\n{ex.Message}", "Error",
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