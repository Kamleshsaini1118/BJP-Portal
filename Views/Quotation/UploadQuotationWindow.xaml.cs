using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp
{
    public partial class UploadQuotationWindow : Window
    {
        public VendorQuotation Quotation { get; private set; }
        public bool IsEditMode { get; }
        private string? _selectedFilePath;

        public UploadQuotationWindow(int? selectedProjectId = null, VendorQuotation? quotationToEdit = null)
        {
            InitializeComponent();
            IsEditMode = quotationToEdit != null;
            Quotation = quotationToEdit ?? new VendorQuotation();

            StatusCombo.ItemsSource = new[]
            {
                "Pending Review", "Approved", "Rejected"
            };

            if (IsEditMode)
            {
                HeaderText.Text = "Edit Quotation";
                SubmitButton.Content = "Save Changes";

                VendorNameBox.Text = Quotation.VendorName;
                QuotationNumberBox.Text = Quotation.QuotationNumber;
                AmountBox.Text = Quotation.QuotationAmount > 0 ? Quotation.QuotationAmount.ToString("0") : "";
                StatusCombo.SelectedItem = Quotation.Status;
                QuotationDatePicker.SelectedDate = Quotation.QuotationDate;
                ValidUntilPicker.SelectedDate = Quotation.ValidUntil;
                RemarksBox.Text = Quotation.Remarks;
                if (!string.IsNullOrWhiteSpace(Quotation.FilePath))
                {
                    _selectedFilePath = Quotation.FilePath;
                    FileNameText.Text = $"Selected: {Path.GetFileName(_selectedFilePath)}";
                }
            }
            else
            {
                HeaderText.Text = "Upload Quotation";
                SubmitButton.Content = "Submit";
                StatusCombo.SelectedIndex = 0;
                QuotationDatePicker.SelectedDate = IndiaTime.Today;
            }

            Loaded += async (s, e) =>
            {
                var projects = await QuotationRepository.GetProjectsAsync();
                ProjectCombo.ItemsSource = projects;

                int projIdToSelect = quotationToEdit?.ProjectId ?? selectedProjectId ?? 0;
                if (projIdToSelect > 0)
                {
                    ProjectCombo.SelectedValue = projIdToSelect;
                }
                else if (projects.Count > 0)
                {
                    ProjectCombo.SelectedIndex = 0;
                }
            };
        }

        private void FileDropZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Quotation File",
                Filter = "All Supported Files (*.pdf;*.png;*.jpg;*.jpeg)|*.pdf;*.png;*.jpg;*.jpeg|PDF Files (*.pdf)|*.pdf|Image Files (*.png;*.jpg)|*.png;*.jpg"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedFilePath = dialog.FileName;
                FileNameText.Text = $"Selected: {Path.GetFileName(_selectedFilePath)}";
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectCombo.SelectedValue is not int projId || projId <= 0)
            {
                MessageBox.Show("Please select a Project.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectCombo.Focus();
                return;
            }

            string vendor = VendorNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(vendor))
            {
                MessageBox.Show("Please enter Vendor Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                VendorNameBox.Focus();
                return;
            }

            string amountText = AmountBox.Text.Replace("₹", "").Replace(",", "").Trim();
            if (string.IsNullOrWhiteSpace(amountText) || !decimal.TryParse(amountText, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid numeric Quotation Amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountBox.Focus();
                return;
            }

            Quotation.ProjectId = projId;
            Quotation.VendorName = vendor;
            Quotation.QuotationNumber = QuotationNumberBox.Text.Trim();
            Quotation.QuotationAmount = amount;
            Quotation.Status = StatusCombo.SelectedItem?.ToString() ?? "Pending Review";
            Quotation.QuotationDate = QuotationDatePicker.SelectedDate ?? IndiaTime.Today;
            Quotation.ValidUntil = ValidUntilPicker.SelectedDate;
            Quotation.Remarks = RemarksBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(_selectedFilePath))
            {
                Quotation.FilePath = _selectedFilePath;
            }

            try
            {
                await QuotationRepository.SaveQuotationAsync(Quotation);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving quotation: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
