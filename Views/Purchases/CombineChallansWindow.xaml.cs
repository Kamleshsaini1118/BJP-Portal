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
    public partial class CombineChallansWindow : Window
    {
        private readonly ObservableCollection<ChallanPickRow> _rows = new();
        private readonly ObservableCollection<InvoiceFileEntry> _invoiceFiles = new();

        public CombineChallansWindow(List<string> vendorNames)
        {
            InitializeComponent();

            MaxHeight = SystemParameters.WorkArea.Height - 40;
            BodyScrollViewer.MaxHeight = SystemParameters.WorkArea.Height - 220;

            VendorCombo.ItemsSource = vendorNames;
            ChallanListControl.ItemsSource = _rows;
            InvoiceFilesControl.ItemsSource = _invoiceFiles;
            InvoiceDatePicker.SelectedDate = IndiaTime.Today;
        }

        private async void VendorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _rows.Clear();
            UpdateSummary();

            var vendor = VendorCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(vendor))
            {
                EmptyHintText.Text = "Select a vendor to see their pending challans.";
                EmptyHintText.Visibility = Visibility.Visible;
                return;
            }

            var pending = await PurchaseRepository.GetPendingChallansByVendorAsync(vendor);
            foreach (var challan in pending)
            {
                var row = new ChallanPickRow(challan);
                row.PropertyChanged += (_, __) => UpdateSummary();
                _rows.Add(row);
            }

            EmptyHintText.Text = "No pending challans for this vendor.";
            EmptyHintText.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateSummary()
        {
            var selected = _rows.Where(r => r.IsSelected).ToList();
            if (selected.Count == 0)
            {
                SummaryBar.Visibility = Visibility.Collapsed;
                return;
            }

            SummaryBar.Visibility = Visibility.Visible;
            SelectedCountText.Text = $"{selected.Count} selected";
            SelectedTotalText.Text = $"₹{selected.Sum(r => r.Challan.Total):N2}";
        }

        private void AttachFilesButton_Click(object sender, RoutedEventArgs e)
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

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: InvoiceFileEntry file }) return;
            _invoiceFiles.Remove(file);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var vendor = VendorCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(vendor))
            {
                MessageBox.Show("Select a vendor before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = _rows.Where(r => r.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Select at least one pending challan to combine.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var invoiceNumber = InvoiceNumberBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                MessageBox.Show("Enter the invoice number before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveButton.IsEnabled = false;
            try
            {
                var invoiceDate = InvoiceDatePicker.SelectedDate;
                var remarks = RemarksBox.Text.Trim();
                var invoiceGroupId = selected.Min(r => r.Challan.Id);

                foreach (var row in selected)
                {
                    await PurchaseRepository.LinkChallanToInvoiceAsync(
                        row.Challan.Id, invoiceNumber, invoiceDate, invoiceGroupId,
                        string.IsNullOrWhiteSpace(remarks) ? null : remarks);
                }

                foreach (var file in _invoiceFiles.Where(f => f.Data != null))
                {
                    foreach (var row in selected)
                    {
                        await PurchaseRepository.AddInvoiceFileAsync(row.Challan.Id, file.FileName, file.Data!);
                    }
                }

                MessageBox.Show($"Invoice {invoiceNumber} linked to {selected.Count} challan(s) from {vendor}.", "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the combined invoice.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SaveButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}