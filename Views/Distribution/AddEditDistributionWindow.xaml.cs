using StockPortalApp.Data;
 using StockPortalApp.Helpers;
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
    /// Interaction logic for AddEditDistributionWindow.xaml
    /// </summary>
    public partial class AddEditDistributionWindow : Window
    {
        private readonly List<Product> _products;
        private readonly List<ProductCategory> _categories;
        private readonly DistributionSummary? _editingDistribution;
        private readonly ObservableCollection<DistributionItemRow> _items = new();
        private readonly List<string> _officeNames;

        private const string AddOfficeSentinel = "+ Add New Office";

        public ObservableCollection<string> CategoryNames { get; } = new();

        /// <summary>Set when Submit succeeds, so the caller can show a receipt.</summary>
        public ReceiptData? CreatedReceipt { get; private set; }

        public AddEditDistributionWindow(
            List<ProductCategory> categories, List<Product> products, List<string> officeNames,
            DistributionSummary? distributionToEdit = null, List<DistributionItemRow>? existingItems = null)
        {
            InitializeComponent();
            DataContext = this;

            MaxHeight = SystemParameters.WorkArea.Height - 40;
            BodyScrollViewer.MaxHeight = SystemParameters.WorkArea.Height - 220;

            _products = products;
            _categories = categories;
            _editingDistribution = distributionToEdit;
            
            CategoryNames.Clear();
            foreach (var c in categories) CategoryNames.Add(c.Name);
            if (!CategoryNames.Contains("+ Add New Category"))
            {
                CategoryNames.Add("+ Add New Category");
            }
            _officeNames = new List<string>(officeNames);

            ItemRowsControl.ItemsSource = _items;
            DispatchDatePicker.SelectedDate = IndiaTime.Today;

            RebuildDestinationCombo(null);

            if (_editingDistribution != null)
            {
                Title = "Edit Distribution";
                HeaderTitle.Text = "Edit Distribution";
                SubmitButton.Content = "Save Changes";

                if (!string.IsNullOrEmpty(_editingDistribution.Destination) && !_officeNames.Contains(_editingDistribution.Destination))
                {
                    _officeNames.Insert(0, _editingDistribution.Destination);
                }
                RebuildDestinationCombo(_editingDistribution.Destination);

                DispatchDatePicker.SelectedDate = _editingDistribution.DistributionDate;
                ReceiverNameBox.Text = _editingDistribution.ReceiverName;
                ReceiverNumberBox.Text = _editingDistribution.ReceiverNumber;
                PositionBox.Text = _editingDistribution.Position;
                RemarkBox.Text = _editingDistribution.Remark;

                foreach (var item in existingItems ?? new List<DistributionItemRow>())
                {
                    var row = CreateDistributionItemRow();
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

        private DistributionItemRow CreateDistributionItemRow()
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

        private void AddItemRow() => _items.Add(CreateDistributionItemRow());

        private void AddItemButton_Click(object sender, RoutedEventArgs e) => AddItemRow();

        // ---- Destination combo (with inline "+ Add New Office") ----

        private void RebuildDestinationCombo(string? select)
        {
            var items = new List<string>(_officeNames) { AddOfficeSentinel };
            DestinationCombo.ItemsSource = items;
            if (select != null) DestinationCombo.SelectedItem = select;
        }

        private void DestinationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DestinationCombo.SelectedItem as string != AddOfficeSentinel) return;

            var dialog = new AddOfficeWindow { Owner = this };
            if (dialog.ShowDialog() == true && dialog.SavedOffice != null)
            {
                _officeNames.Add(dialog.SavedOffice.Name);
                RebuildDestinationCombo(dialog.SavedOffice.Name);
            }
            else
            {
                DestinationCombo.SelectedItem = null;
            }
        }

        // ---- Items ----

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DistributionItemRow row }) return;
            _items.Remove(row);
        }

        // ---- Submit ----

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var destination = DestinationCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(destination) || destination == AddOfficeSentinel)
            {
                MessageBox.Show("Select or add a destination office before saving.", "Missing information",
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

            var dispatchDate = DispatchDatePicker.SelectedDate ?? IndiaTime.Today;
            var receiverName = ReceiverNameBox.Text.Trim();
            var receiverNumber = ReceiverNumberBox.Text.Trim();
            var position = PositionBox.Text.Trim();
            var remark = RemarkBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(receiverNumber) && !NumericInput.IsValidMobileNumber(receiverNumber, isOptional: true))
            {
                MessageBox.Show(
                    "Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.",
                    "Invalid Mobile Number",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReceiverNumberBox.Focus();
                return;
            }

            SubmitButton.IsEnabled = false;
            try
            {
                var (id, shipmentId) = await DistributionRepository.SaveHeaderAsync(
                    _editingDistribution?.Id, destination, dispatchDate, receiverName, receiverNumber, position, remark);

                await DistributionRepository.ClearItemsAsync(id);
                foreach (var row in rows)
                {
                    var qty = decimal.Parse(row.Qty);
                    await DistributionRepository.AddItemAsync(id, row.Category, row.ProductName, qty);
                }

                var displayShipmentId = string.IsNullOrEmpty(shipmentId) ? _editingDistribution!.ShipmentId : shipmentId;

                CreatedReceipt = new ReceiptData
                {
                    Type = "dispatched",
                    BatchId = displayShipmentId,
                    IdLabel = "Shipment ID",
                    Subtitle = "Export — Distribution Receipt",
                    PersonName = receiverName,
                    PersonNumber = receiverNumber,
                    Items = rows.Select(r => new ReceiptItem { Name = r.ProductName, Qty = decimal.Parse(r.Qty) }).ToList(),
                    Date = dispatchDate.ToString("dd MMM yyyy"),
                    ExtraMeta = new List<ReceiptExtraField>
                    {
                        new() { Label = "Destination", Value = destination },
                        new() { Label = "Position", Value = position },
                        new() { Label = "Remark", Value = remark },
                    }
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the distribution.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitButton.IsEnabled = true;
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
