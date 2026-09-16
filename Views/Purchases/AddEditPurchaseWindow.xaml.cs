using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp
{
    public partial class AddEditPurchaseWindow : Window
    {
        private readonly List<Product> _products;
        private readonly Purchase? _editingPurchase;
        private readonly int? _editingDistributionId;
        private readonly ObservableCollection<PurchaseOrderItemRow> _purchaseItems = new();
        private readonly ObservableCollection<DistributionItemRow> _distItems = new();
        private readonly ObservableCollection<InvoiceFileEntry> _invoiceFiles = new();
        private readonly List<int> _deletedInvoiceFileIds = new();
        private readonly List<string> _vendorNames;
        private readonly List<string> _officeNames;

        private const string AddVendorSentinel = "+ Add New Vendor";
        private const string AddOfficeSentinel = "+ Add New Office";

        public List<string> CategoryNames { get; }
        public List<decimal> GstOptions { get; } = new() { 0, 5, 12, 18, 28 };

        /// <summary>Set when Submit succeeds, so the caller can show a combined receipt.</summary>
        public int? SavedPurchaseId { get; private set; }

        public AddEditPurchaseWindow(
            List<string> vendorNames, List<ProductCategory> categories, List<Product> products,
            List<string> officeNames,
            Purchase? purchaseToEdit = null,
            List<PurchaseOrderItemRow>? existingPurchaseItems = null,
            (string? ShipmentId, string? Destination, DateTime? Date, string? ReceiverName, string? ReceiverNumber, string? Position, string? Remark, List<(string? Category, string ProductName, decimal Qty)> Items)? existingDistribution = null,
            List<InvoiceFileEntry>? existingInvoiceFiles = null,
            List<string>? poReferences = null)
        {
            InitializeComponent();
            DataContext = this;

            // Cap the window to the actual screen's usable height so the footer
            // (Cancel/Submit) never gets pushed off-screen on smaller displays.
            MaxHeight = SystemParameters.WorkArea.Height - 40;
            BodyScrollViewer.MaxHeight = SystemParameters.WorkArea.Height - 220;

            _products = products;
            _editingPurchase = purchaseToEdit;
            _editingDistributionId = purchaseToEdit?.DistributionId;
            CategoryNames = categories.Select(c => c.Name).ToList();
            _vendorNames = new List<string>(vendorNames);
            _officeNames = new List<string>(officeNames);

            TypeCombo.ItemsSource = new[] { "Direct", "In Store" };
            PurchaseItemRowsControl.ItemsSource = _purchaseItems;
            DistItemRowsControl.ItemsSource = _distItems;
            InvoiceFilesControl.ItemsSource = _invoiceFiles;
            PoRefCombo.ItemsSource = poReferences ?? new List<string>();
            ReceiptBasisCombo.ItemsSource = new[] { "Invoice", "Challan" };
            ReceiptBasisCombo.SelectedIndex = 0;
            PurchaseDatePicker.SelectedDate = IndiaTime.Today;

            foreach (var file in existingInvoiceFiles ?? new List<InvoiceFileEntry>())
            {
                _invoiceFiles.Add(file);
            }

            RebuildVendorCombo(null);
            RebuildDestinationCombo(null);

            if (_editingPurchase != null)
            {
                Title = "Edit Purchase";
                HeaderTitle.Text = "Edit Purchase";
                SubmitButton.Content = "Save Changes";

                PoRefCombo.Text = _editingPurchase.PoReference;
                InvoiceBox.Text = _editingPurchase.InvoiceNumber;
                ChallanNumberBox.Text = _editingPurchase.ChallanNumber;
                ChallanDatePicker.SelectedDate = _editingPurchase.ChallanDate;
                PurchaseDatePicker.SelectedDate = _editingPurchase.PurchaseDate;

                // Set last so ReceiptBasisCombo_SelectionChanged toggles the right fields
                // with everything already prefilled, instead of racing them.
                ReceiptBasisCombo.SelectedItem = _editingPurchase.ReceiptBasis;

                if (!string.IsNullOrEmpty(_editingPurchase.VendorName) && !_vendorNames.Contains(_editingPurchase.VendorName))
                {
                    _vendorNames.Insert(0, _editingPurchase.VendorName);
                }
                RebuildVendorCombo(_editingPurchase.VendorName);

                ReceiverNameBox.Text = _editingPurchase.ReceiverName;
                ReceiverNumberBox.Text = _editingPurchase.ReceiverNumber;
                PaymentModeBox.Text = _editingPurchase.PaymentMode;
                RemarksBox.Text = _editingPurchase.Remarks;

                foreach (var item in existingPurchaseItems ?? new List<PurchaseOrderItemRow>())
                {
                    var row = new PurchaseOrderItemRow(_products) { Category = item.Category };
                    row.ProductName = item.ProductName;
                    row.Qty = item.Qty;
                    row.Price = item.Price;
                    row.GstPercent = item.GstPercent;
                    row.PropertyChanged += (_, __) => RecalculateGrandTotal();
                    _purchaseItems.Add(row);
                }

                if (_editingPurchase.PurchaseType == "Direct" && existingDistribution.HasValue)
                {
                    var d = existingDistribution.Value;

                    if (!string.IsNullOrEmpty(d.Destination) && !_officeNames.Contains(d.Destination))
                    {
                        _officeNames.Insert(0, d.Destination);
                    }
                    RebuildDestinationCombo(d.Destination);

                    DispatchDatePicker.SelectedDate = d.Date;
                    DistReceiverNameBox.Text = d.ReceiverName;
                    DistReceiverNumberBox.Text = d.ReceiverNumber;
                    DistPositionBox.Text = d.Position;
                    DistRemarkBox.Text = d.Remark;

                    foreach (var item in d.Items)
                    {
                        var row = new DistributionItemRow(_products) { Category = item.Category };
                        row.ProductName = item.ProductName;
                        row.Qty = item.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        _distItems.Add(row);
                    }
                }

                // Set the type LAST so the SelectionChanged handler reveals the sections
                // with all the prefilled rows already in place, instead of racing them.
                TypeCombo.SelectedItem = _editingPurchase.PurchaseType;
            }
            else
            {
                AddPurchaseItemRow();
            }

            RecalculateGrandTotal();
        }

        // ---- Vendor combo (with inline "+ Add New Vendor") ----

        private void RebuildVendorCombo(string? select)
        {
            var items = new List<string>(_vendorNames) { AddVendorSentinel };
            VendorCombo.ItemsSource = items;
            if (select != null) VendorCombo.SelectedItem = select;
        }

        private void VendorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VendorCombo.SelectedItem as string != AddVendorSentinel) return;

            var dialog = new AddEditVendorWindow { Owner = this };
            if (dialog.ShowDialog() == true && dialog.SavedVendor != null)
            {
                _vendorNames.Add(dialog.SavedVendor.Name);
                RebuildVendorCombo(dialog.SavedVendor.Name);
            }
            else
            {
                VendorCombo.SelectedItem = null;
            }
        }

        // ---- Received Against: Invoice vs Challan ----

        private void ReceiptBasisCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var isChallan = ReceiptBasisCombo.SelectedItem as string == "Challan";

            InvoiceNumberLabel.Visibility = isChallan ? Visibility.Collapsed : Visibility.Visible;
            InvoiceBox.Visibility = isChallan ? Visibility.Collapsed : Visibility.Visible;
            ChallanNumberLabel.Visibility = isChallan ? Visibility.Visible : Visibility.Collapsed;
            ChallanNumberBox.Visibility = isChallan ? Visibility.Visible : Visibility.Collapsed;
            ChallanDateRow.Visibility = isChallan ? Visibility.Visible : Visibility.Collapsed;
            ChallanHintBorder.Visibility = isChallan ? Visibility.Visible : Visibility.Collapsed;
        }

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

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var type = TypeCombo.SelectedItem as string;

            PurchaseDetailsSection.Visibility = string.IsNullOrEmpty(type) ? Visibility.Collapsed : Visibility.Visible;
            DistributionDetailsSection.Visibility = (type == "Direct") ? Visibility.Visible : Visibility.Collapsed;

            // Only auto-add a starting distribution row the first time it becomes visible for a brand-new purchase.
            if (_editingPurchase == null && type == "Direct" && _distItems.Count == 0)
            {
                AddDistItemRow();
            }
        }

        // ---- Purchase items ----

        private void AddPurchaseItemRow()
        {
            var row = new PurchaseOrderItemRow(_products) { GstPercent = 18 };
            row.PropertyChanged += (_, __) => RecalculateGrandTotal();
            _purchaseItems.Add(row);
            RecalculateGrandTotal();
        }

        private void RecalculateGrandTotal()
        {
            var total = _purchaseItems.Sum(r => r.TotalValue);
            GrandTotalText.Text = $"₹{total:N2}";
        }

        private void AddPurchaseItemButton_Click(object sender, RoutedEventArgs e) => AddPurchaseItemRow();

        private void RemovePurchaseItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: PurchaseOrderItemRow row }) return;
            _purchaseItems.Remove(row);
            RecalculateGrandTotal();
        }

        // ---- Distribution items ----

        private void AddDistItemRow()
        {
            var row = new DistributionItemRow(_products);
            _distItems.Add(row);
        }

        private void AddDistItemButton_Click(object sender, RoutedEventArgs e) => AddDistItemRow();

        private void RemoveDistItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DistributionItemRow row }) return;
            _distItems.Remove(row);
        }

        // ---- Invoice files ----

        private void AttachInvoiceFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Images and PDF|*.jpg;*.jpeg;*.png;*.pdf|All files|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            foreach (var path in dialog.FileNames)
            {
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    _invoiceFiles.Add(new InvoiceFileEntry
                    {
                        Id = null,
                        FileName = Path.GetFileName(path),
                        Data = bytes
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not read \"{Path.GetFileName(path)}\".\n\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveInvoiceFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: InvoiceFileEntry file }) return;

            if (file.Id.HasValue)
            {
                _deletedInvoiceFileIds.Add(file.Id.Value);
            }
            _invoiceFiles.Remove(file);
        }

        // ---- Submit ----

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var type = TypeCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Select a purchase type before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var vendor = VendorCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(vendor) || vendor == AddVendorSentinel)
            {
                MessageBox.Show("Select a vendor before saving the purchase details.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var receiptBasis = ReceiptBasisCombo.SelectedItem as string ?? "Invoice";
            if (receiptBasis == "Challan" && string.IsNullOrWhiteSpace(ChallanNumberBox.Text))
            {
                MessageBox.Show("Enter the challan number before saving — this purchase is being recorded without an invoice.",
                    "Missing information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var purchaseRows = _purchaseItems
                .Where(r => !string.IsNullOrWhiteSpace(r.ProductName) && decimal.TryParse(r.Qty, out var q) && q > 0)
                .ToList();
            if (purchaseRows.Count == 0)
            {
                MessageBox.Show("Add at least one purchase item with a product and quantity.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string? destination = null;
            List<DistributionItemRow> distRows = new();
            if (type == "Direct")
            {
                destination = DestinationCombo.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(destination) || destination == AddOfficeSentinel)
                {
                    MessageBox.Show("Select a destination office before saving.", "Missing information",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                distRows = _distItems
                    .Where(r => !string.IsNullOrWhiteSpace(r.ProductName) && decimal.TryParse(r.Qty, out var q) && q > 0)
                    .ToList();
                if (distRows.Count == 0)
                {
                    MessageBox.Show("Add at least one distribution item with a quantity before saving.", "Missing information",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            SubmitButton.IsEnabled = false;
            try
            {
                int? distributionId = _editingDistributionId;

                if (type == "Direct")
                {
                    var dispatchDate = DispatchDatePicker.SelectedDate ?? IndiaTime.Today;
                    var distReceiverName = DistReceiverNameBox.Text.Trim();
                    var distReceiverNumber = DistReceiverNumberBox.Text.Trim();
                    var distPosition = DistPositionBox.Text.Trim();
                    var distRemark = DistRemarkBox.Text.Trim();

                    if (distributionId.HasValue)
                    {
                        await PurchaseRepository.UpdateDistributionHeaderAsync(
                            distributionId.Value, destination!, dispatchDate, distReceiverName, distReceiverNumber, distPosition, distRemark);
                        await PurchaseRepository.ClearDistributionItemsAsync(distributionId.Value);
                    }
                    else
                    {
                        var (newDistId, _) = await PurchaseRepository.AddDistributionAsync(
                            destination!, dispatchDate, distReceiverName, distReceiverNumber, distPosition, distRemark);
                        distributionId = newDistId;
                    }

                    foreach (var row in distRows)
                    {
                        var qty = decimal.Parse(row.Qty);
                        await PurchaseRepository.AddDistributionItemAsync(distributionId.Value, row.Category, row.ProductName, qty);
                    }
                }
                else
                {
                    distributionId = null; // "In Store" purchases have no linked distribution
                }

                var poRef = (PoRefCombo.Text ?? "").Trim();
                var paymentMode = PaymentModeBox.Text.Trim();
                var remarks = RemarksBox.Text.Trim();
                var purchaseDate = PurchaseDatePicker.SelectedDate ?? IndiaTime.Today;
                var receiverName = ReceiverNameBox.Text.Trim();
                var receiverNumber = ReceiverNumberBox.Text.Trim();

                // Figure out invoice/invoiceStatus/challan fields:
                // - Plain invoice purchases are always "Invoiced".
                // - New or basis-switched challan purchases start "Pending" with no invoice yet.
                // - A challan purchase already linked to an invoice (via Combine Challans) keeps
                //   that link when the person is just editing other fields on it.
                string? invoice;
                string? challanNumber = null;
                DateTime? challanDate = null;
                DateTime? invoiceDate = _editingPurchase?.InvoiceDate;
                int? invoiceGroupId = _editingPurchase?.InvoiceGroupId;
                string invoiceStatus;

                var wasLinkedChallan = _editingPurchase != null
                    && _editingPurchase.ReceiptBasis == "Challan"
                    && _editingPurchase.InvoiceStatus == "Invoiced";

                if (receiptBasis == "Challan")
                {
                    challanNumber = ChallanNumberBox.Text.Trim();
                    challanDate = ChallanDatePicker.SelectedDate;

                    if (wasLinkedChallan)
                    {
                        invoice = _editingPurchase!.InvoiceNumber;
                        invoiceStatus = "Invoiced";
                    }
                    else
                    {
                        invoice = null;
                        invoiceStatus = "Pending";
                        invoiceGroupId = null;
                        invoiceDate = null;
                    }
                }
                else
                {
                    invoice = InvoiceBox.Text.Trim();
                    invoiceStatus = "Invoiced";
                    invoiceGroupId = null;
                    invoiceDate = null;
                }

                var purchaseId = await PurchaseRepository.SaveHeaderAsync(
                    _editingPurchase?.Id, type,
                    string.IsNullOrWhiteSpace(poRef) ? null : poRef,
                    string.IsNullOrWhiteSpace(invoice) ? null : invoice,
                    vendor,
                    string.IsNullOrWhiteSpace(paymentMode) ? null : paymentMode,
                    purchaseDate, receiverName, receiverNumber, remarks, distributionId,
                    receiptBasis, challanNumber, challanDate, invoiceDate, invoiceStatus, invoiceGroupId);

                await PurchaseRepository.ClearItemsAsync(purchaseId);
                foreach (var row in purchaseRows)
                {
                    var qty = decimal.Parse(row.Qty);
                    var price = decimal.TryParse(row.Price, out var p) ? p : 0;
                    await PurchaseRepository.AddItemAsync(purchaseId, row.Category, row.ProductName, qty, price, row.GstPercent);
                }

                foreach (var deletedId in _deletedInvoiceFileIds)
                {
                    await PurchaseRepository.DeleteInvoiceFileAsync(deletedId);
                }
                foreach (var file in _invoiceFiles.Where(f => f.Data != null))
                {
                    await PurchaseRepository.AddInvoiceFileAsync(purchaseId, file.FileName, file.Data!);
                }

                SavedPurchaseId = purchaseId;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the purchase.\n\n{ex.Message}", "Error",
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











// using Microsoft.Win32;
// using StockPortalApp.Data;
// using StockPortalApp.Helpers;
// using StockPortalApp.Models;
// using System;
// using System.Collections.Generic;
// using System.Collections.ObjectModel;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using System.Windows;
// using System.Windows.Controls;
// using System.Windows.Data;
// using System.Windows.Documents;
// using System.Windows.Input;
// using System.Windows.Media;
// using System.Windows.Media.Imaging;
// using System.Windows.Shapes;

// namespace StockPortalApp
// {
//     public partial class AddEditPurchaseWindow : Window
//     {
//         private readonly List<Product> _products;
//         private readonly Purchase? _editingPurchase;
//         private readonly int? _editingDistributionId;
//         private readonly ObservableCollection<PurchaseOrderItemRow> _purchaseItems = new();
//         private readonly ObservableCollection<DistributionItemRow> _distItems = new();
//         private readonly ObservableCollection<InvoiceFileEntry> _invoiceFiles = new();
//         private readonly List<int> _deletedInvoiceFileIds = new();
//         private readonly List<string> _vendorNames;
//         private readonly List<string> _officeNames;

//         private const string AddVendorSentinel = "+ Add New Vendor";
//         private const string AddOfficeSentinel = "+ Add New Office";

//         public ObservableCollection<string> CategoryNames { get; } = new();
//         public List<decimal> GstOptions { get; } = new() { 0, 5, 12, 18, 28 };

//         /// <summary>Set when Submit succeeds, so the caller can show a combined receipt.</summary>
//         public int? SavedPurchaseId { get; private set; }

//         public AddEditPurchaseWindow(
//                     List<string> vendorNames, List<ProductCategory> categories, List<Product> products,
//                     List<string> officeNames,
//                     Purchase? purchaseToEdit = null,
//                     List<PurchaseOrderItemRow>? existingPurchaseItems = null,
//                     (string? ShipmentId, string? Destination, DateTime? Date, string? ReceiverName, string? ReceiverNumber, string? Position, string? Remark, List<(string? Category, string ProductName, decimal Qty)> Items)? existingDistribution = null,
//                     List<InvoiceFileEntry>? existingInvoiceFiles = null,
//                     List<string>? poReferences = null)
//         {
//             InitializeComponent();
//             DataContext = this;

//             // Cap the window to the actual screen's usable height so the footer
//             // (Cancel/Submit) never gets pushed off-screen on smaller displays.
//             MaxHeight = SystemParameters.WorkArea.Height - 40;
//             BodyScrollViewer.MaxHeight = SystemParameters.WorkArea.Height - 220;

//             _products = products;
//             _editingPurchase = purchaseToEdit;
//             _editingDistributionId = purchaseToEdit?.DistributionId;

//             CategoryNames.Clear();
//             foreach (var c in categories) CategoryNames.Add(c.Name);
//             if (!CategoryNames.Contains("+ Add New Category"))
//             {
//                 CategoryNames.Add("+ Add New Category");
//             }
//             _vendorNames = new List<string>(vendorNames);
//             _officeNames = new List<string>(officeNames);

//             TypeCombo.ItemsSource = new[] { "Direct", "In Store" };
//             PurchaseItemRowsControl.ItemsSource = _purchaseItems;
//             DistItemRowsControl.ItemsSource = _distItems;
//             InvoiceFilesControl.ItemsSource = _invoiceFiles;
//             PoRefCombo.ItemsSource = poReferences ?? new List<string>();
//             PurchaseDatePicker.SelectedDate = IndiaTime.Today;

//             foreach (var file in existingInvoiceFiles ?? new List<InvoiceFileEntry>())
//             {
//                 _invoiceFiles.Add(file);
//             }

//             RebuildVendorCombo(null);
//             RebuildDestinationCombo(null);

//             if (_editingPurchase != null)
//             {
//                 Title = "Edit Purchase";
//                 HeaderTitle.Text = "Edit Purchase";
//                 SubmitButton.Content = "Save Changes";

//                 PoRefCombo.Text = _editingPurchase.PoReference;
//                 InvoiceBox.Text = _editingPurchase.InvoiceNumber;
//                 PurchaseDatePicker.SelectedDate = _editingPurchase.PurchaseDate;

//                 if (!string.IsNullOrEmpty(_editingPurchase.VendorName) && !_vendorNames.Contains(_editingPurchase.VendorName))
//                 {
//                     _vendorNames.Insert(0, _editingPurchase.VendorName);
//                 }
//                 RebuildVendorCombo(_editingPurchase.VendorName);

//                 ReceiverNameBox.Text = _editingPurchase.ReceiverName;
//                 ReceiverNumberBox.Text = _editingPurchase.ReceiverNumber;
//                 PaymentModeBox.Text = _editingPurchase.PaymentMode;
//                 RemarksBox.Text = _editingPurchase.Remarks;

//                 foreach (var item in existingPurchaseItems ?? new List<PurchaseOrderItemRow>())
//                 {
//                     var row = new PurchaseOrderItemRow(_products) { Category = item.Category };
//                     row.ProductName = item.ProductName;
//                     row.Qty = item.Qty;
//                     row.Price = item.Price;
//                     row.GstPercent = item.GstPercent;
//                     row.PropertyChanged += (_, __) => RecalculateGrandTotal();
//                     _purchaseItems.Add(row);
//                 }

//                 if (_editingPurchase.PurchaseType == "Direct" && existingDistribution.HasValue)
//                 {
//                     var d = existingDistribution.Value;

//                     if (!string.IsNullOrEmpty(d.Destination) && !_officeNames.Contains(d.Destination))
//                     {
//                         _officeNames.Insert(0, d.Destination);
//                     }
//                     RebuildDestinationCombo(d.Destination);

//                     DispatchDatePicker.SelectedDate = d.Date;
//                     DistReceiverNameBox.Text = d.ReceiverName;
//                     DistReceiverNumberBox.Text = d.ReceiverNumber;
//                     DistPositionBox.Text = d.Position;
//                     DistRemarkBox.Text = d.Remark;

//                     foreach (var item in d.Items)
//                     {
//                         var row = new DistributionItemRow(_products) { Category = item.Category };
//                         row.ProductName = item.ProductName;
//                         row.Qty = item.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture);
//                         _distItems.Add(row);
//                     }
//                 }

//                 // Set the type LAST so the SelectionChanged handler reveals the sections
//                 // with all the prefilled rows already in place, instead of racing them.
//                 TypeCombo.SelectedItem = _editingPurchase.PurchaseType;
//             }
//             else
//             {
//                 AddPurchaseItemRow();
//             }

//             RecalculateGrandTotal();
//         }

//         // ---- Vendor combo (with inline "+ Add New Vendor") ----

//         private void RebuildVendorCombo(string? select)
//         {
//             var items = new List<string>(_vendorNames) { AddVendorSentinel };
//             VendorCombo.ItemsSource = items;
//             if (select != null) VendorCombo.SelectedItem = select;
//         }

//         private void VendorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//         {
//             if (VendorCombo.SelectedItem as string != AddVendorSentinel) return;

//             var dialog = new AddEditVendorWindow { Owner = this };
//             if (dialog.ShowDialog() == true && dialog.SavedVendor != null)
//             {
//                 _vendorNames.Add(dialog.SavedVendor.Name);
//                 RebuildVendorCombo(dialog.SavedVendor.Name);
//             }
//             else
//             {
//                 VendorCombo.SelectedItem = null;
//             }
//         }

//         // ---- Destination combo (with inline "+ Add New Office") ----

//         private void RebuildDestinationCombo(string? select)
//         {
//             var items = new List<string>(_officeNames) { AddOfficeSentinel };
//             DestinationCombo.ItemsSource = items;
//             if (select != null) DestinationCombo.SelectedItem = select;
//         }

//         private void DestinationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//         {
//             if (DestinationCombo.SelectedItem as string != AddOfficeSentinel) return;

//             var dialog = new AddOfficeWindow { Owner = this };
//             if (dialog.ShowDialog() == true && dialog.SavedOffice != null)
//             {
//                 _officeNames.Add(dialog.SavedOffice.Name);
//                 RebuildDestinationCombo(dialog.SavedOffice.Name);
//             }
//             else
//             {
//                 DestinationCombo.SelectedItem = null;
//             }
//         }

//         private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
//         {
//             var type = TypeCombo.SelectedItem as string;

//             PurchaseDetailsSection.Visibility = string.IsNullOrEmpty(type) ? Visibility.Collapsed : Visibility.Visible;
//             DistributionDetailsSection.Visibility = (type == "Direct") ? Visibility.Visible : Visibility.Collapsed;

//             // Only auto-add a starting distribution row the first time it becomes visible for a brand-new purchase.
//             if (_editingPurchase == null && type == "Direct" && _distItems.Count == 0)
//             {
//                 AddDistItemRow();
//             }
//         }

//         // ---- Purchase items ----

//         private void AddPurchaseItemRow()
//         {
//             var row = new PurchaseOrderItemRow(_products) { GstPercent = 18 };
//             row.OnAddNewCategoryRequested = (r) => OpenAddProductModal(r);
//             row.OnAddNewItemRequested = (r) => OpenAddProductModal(r);
//             row.PropertyChanged += (_, __) => RecalculateGrandTotal();
//             _purchaseItems.Add(row);
//             RecalculateGrandTotal();
//         }

//         private async void OpenAddProductModal(PurchaseOrderItemRow targetRow)
//         {
//             var categories = await ProductRepository.GetCategoriesAsync();
//             var dialog = new AddEditProductWindow(categories) { Owner = this };
//             if (dialog.ShowDialog() == true && dialog.SavedProduct != null)
//             {
//                 var p = dialog.SavedProduct;
//                 var freshCats = await ProductRepository.GetCategoriesAsync();
//                 var freshProds = await ProductRepository.GetProductsAsync();

//                 _products.Clear();
//                 _products.AddRange(freshProds);

//                 CategoryNames.Clear();
//                 foreach (var cat in freshCats)
//                 {
//                     CategoryNames.Add(cat.Name);
//                 }
//                 if (!CategoryNames.Contains("+ Add New Category"))
//                 {
//                     CategoryNames.Add("+ Add New Category");
//                 }

//                 foreach (var r in _purchaseItems)
//                 {
//                     r.UpdateProducts(_products);
//                 }

//                 targetRow.Category = p.Category;
//                 targetRow.ProductName = p.Name;
//             }
//         }

//         private void RecalculateGrandTotal()
//         {
//             var total = _purchaseItems.Sum(r => r.TotalValue);
//             GrandTotalText.Text = $"₹{total:N2}";
//         }

//         private void AddPurchaseItemButton_Click(object sender, RoutedEventArgs e) => AddPurchaseItemRow();

//         private void RemovePurchaseItemButton_Click(object sender, RoutedEventArgs e)
//         {
//             if (sender is not Button { DataContext: PurchaseOrderItemRow row }) return;
//             _purchaseItems.Remove(row);
//             RecalculateGrandTotal();
//         }

//         // ---- Distribution items ----

//         private void AddDistItemRow()
//         {
//             var row = new DistributionItemRow(_products);
//             row.OnAddNewCategoryRequested = (r) => OpenAddProductModalForDist(r);
//             row.OnAddNewItemRequested = (r) => OpenAddProductModalForDist(r);
//             _distItems.Add(row);
//         }

//         private async void OpenAddProductModalForDist(DistributionItemRow targetRow)
//         {
//             var categories = await ProductRepository.GetCategoriesAsync();
//             var dialog = new AddEditProductWindow(categories) { Owner = this };
//             if (dialog.ShowDialog() == true && dialog.SavedProduct != null)
//             {
//                 var p = dialog.SavedProduct;
//                 var freshCats = await ProductRepository.GetCategoriesAsync();
//                 var freshProds = await ProductRepository.GetProductsAsync();

//                 _products.Clear();
//                 _products.AddRange(freshProds);

//                 CategoryNames.Clear();
//                 foreach (var cat in freshCats)
//                 {
//                     CategoryNames.Add(cat.Name);
//                 }
//                 if (!CategoryNames.Contains("+ Add New Category"))
//                 {
//                     CategoryNames.Add("+ Add New Category");
//                 }

//                 foreach (var r in _distItems)
//                 {
//                     r.UpdateProducts(_products);
//                 }

//                 targetRow.Category = p.Category;
//                 targetRow.ProductName = p.Name;
//             }
//         }

//         private void AddDistItemButton_Click(object sender, RoutedEventArgs e) => AddDistItemRow();

//         private void RemoveDistItemButton_Click(object sender, RoutedEventArgs e)
//         {
//             if (sender is not Button { DataContext: DistributionItemRow row }) return;
//             _distItems.Remove(row);
//         }

//         // ---- Invoice files ----

//         private void AttachInvoiceFilesButton_Click(object sender, RoutedEventArgs e)
//         {
//             var dialog = new OpenFileDialog
//             {
//                 Multiselect = true,
//                 Filter = "Images and PDF|*.jpg;*.jpeg;*.png;*.pdf|All files|*.*"
//             };

//             if (dialog.ShowDialog() != true) return;

//             foreach (var path in dialog.FileNames)
//             {
//                 try
//                 {
//                     var bytes = File.ReadAllBytes(path);
//                     _invoiceFiles.Add(new InvoiceFileEntry
//                     {
//                         Id = null,
//                         FileName = System.IO.Path.GetFileName(path),
//                         Data = bytes
//                     });
//                 }
//                 catch (Exception ex)
//                 {
//                     MessageBox.Show($"Could not read \"{System.IO.Path.GetFileName(path)}\".\n\n{ex.Message}", "Error",
//                         MessageBoxButton.OK, MessageBoxImage.Error);
//                 }
//             }
//         }

//         private void RemoveInvoiceFileButton_Click(object sender, RoutedEventArgs e)
//         {
//             if (sender is not Button { DataContext: InvoiceFileEntry file }) return;

//             if (file.Id.HasValue)
//             {
//                 _deletedInvoiceFileIds.Add(file.Id.Value);
//             }
//             _invoiceFiles.Remove(file);
//         }

//         // ---- Submit ----

//         private async void SubmitButton_Click(object sender, RoutedEventArgs e)
//         {
//             var type = TypeCombo.SelectedItem as string;
//             if (string.IsNullOrWhiteSpace(type))
//             {
//                 MessageBox.Show("Select a purchase type before saving.", "Missing information",
//                     MessageBoxButton.OK, MessageBoxImage.Warning);
//                 return;
//             }

//             var vendor = VendorCombo.SelectedItem as string;
//             if (string.IsNullOrWhiteSpace(vendor) || vendor == AddVendorSentinel)
//             {
//                 MessageBox.Show("Select a vendor before saving the purchase details.", "Missing information",
//                     MessageBoxButton.OK, MessageBoxImage.Warning);
//                 return;
//             }

//             var purchaseRows = _purchaseItems
//                 .Where(r => !string.IsNullOrWhiteSpace(r.ProductName) && decimal.TryParse(r.Qty, out var q) && q > 0)
//                 .ToList();
//             if (purchaseRows.Count == 0)
//             {
//                 MessageBox.Show("Add at least one purchase item with a product and quantity.", "Missing information",
//                     MessageBoxButton.OK, MessageBoxImage.Warning);
//                 return;
//             }

//             string? destination = null;
//             List<DistributionItemRow> distRows = new();
//             if (type == "Direct")
//             {
//                 destination = DestinationCombo.SelectedItem as string;
//                 if (string.IsNullOrWhiteSpace(destination) || destination == AddOfficeSentinel)
//                 {
//                     MessageBox.Show("Select a destination office before saving.", "Missing information",
//                         MessageBoxButton.OK, MessageBoxImage.Warning);
//                     return;
//                 }

//                 distRows = _distItems
//                     .Where(r => !string.IsNullOrWhiteSpace(r.ProductName) && decimal.TryParse(r.Qty, out var q) && q > 0)
//                     .ToList();
//                 if (distRows.Count == 0)
//                 {
//                     MessageBox.Show("Add at least one distribution item with a quantity before saving.", "Missing information",
//                         MessageBoxButton.OK, MessageBoxImage.Warning);
//                     return;
//                 }
//             }

//             SubmitButton.IsEnabled = false;
//             try
//             {
//                 int? distributionId = _editingDistributionId;

//                 if (type == "Direct")
//                 {
//                     var dispatchDate = DispatchDatePicker.SelectedDate ?? IndiaTime.Today;
//                     var distReceiverName = DistReceiverNameBox.Text.Trim();
//                     var distReceiverNumber = DistReceiverNumberBox.Text.Trim();
//                     var distPosition = DistPositionBox.Text.Trim();
//                     var distRemark = DistRemarkBox.Text.Trim();

//                     if (distributionId.HasValue)
//                     {
//                         await PurchaseRepository.UpdateDistributionHeaderAsync(
//                             distributionId.Value, destination!, dispatchDate, distReceiverName, distReceiverNumber, distPosition, distRemark);
//                         await PurchaseRepository.ClearDistributionItemsAsync(distributionId.Value);
//                     }
//                     else
//                     {
//                         var (newDistId, _) = await PurchaseRepository.AddDistributionAsync(
//                             destination!, dispatchDate, distReceiverName, distReceiverNumber, distPosition, distRemark);
//                         distributionId = newDistId;
//                     }

//                     foreach (var row in distRows)
//                     {
//                         var qty = decimal.Parse(row.Qty);
//                         await PurchaseRepository.AddDistributionItemAsync(distributionId.Value, row.Category, row.ProductName, qty);
//                     }
//                 }
//                 else
//                 {
//                     distributionId = null; // "In Store" purchases have no linked distribution
//                 }

//                 var poRef = (PoRefCombo.Text ?? "").Trim();
//                 var invoice = InvoiceBox.Text.Trim();
//                 var paymentMode = PaymentModeBox.Text.Trim();
//                 var remarks = RemarksBox.Text.Trim();
//                 var purchaseDate = PurchaseDatePicker.SelectedDate ?? IndiaTime.Today;
//                 var receiverName = ReceiverNameBox.Text.Trim();
//                 var receiverNumber = ReceiverNumberBox.Text.Trim();

//                 var purchaseId = await PurchaseRepository.SaveHeaderAsync(
//                     _editingPurchase?.Id, type,
//                     string.IsNullOrWhiteSpace(poRef) ? null : poRef,
//                     string.IsNullOrWhiteSpace(invoice) ? null : invoice,
//                     vendor,
//                     string.IsNullOrWhiteSpace(paymentMode) ? null : paymentMode,
//                     purchaseDate, receiverName, receiverNumber, remarks, distributionId);

//                 await PurchaseRepository.ClearItemsAsync(purchaseId);
//                 foreach (var row in purchaseRows)
//                 {
//                     var qty = decimal.Parse(row.Qty);
//                     var price = decimal.TryParse(row.Price, out var p) ? p : 0;
//                     await PurchaseRepository.AddItemAsync(purchaseId, row.Category, row.ProductName, qty, price, row.GstPercent);
//                 }

//                 foreach (var deletedId in _deletedInvoiceFileIds)
//                 {
//                     await PurchaseRepository.DeleteInvoiceFileAsync(deletedId);
//                 }
//                 foreach (var file in _invoiceFiles.Where(f => f.Data != null))
//                 {
//                     await PurchaseRepository.AddInvoiceFileAsync(purchaseId, file.FileName, file.Data!);
//                 }

//                 SavedPurchaseId = purchaseId;
//                 DialogResult = true;
//                 Close();
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Could not save the purchase.\n\n{ex.Message}", "Error",
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//                 SubmitButton.IsEnabled = true;
//             }
//         }

//         private void CancelButton_Click(object sender, RoutedEventArgs e)
//         {
//             DialogResult = false;
//             Close();
//         }
//     }
// }
