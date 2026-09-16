using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for AddEditVendorWindow.xaml
    /// </summary>
    public partial class AddEditVendorWindow : Window
    {
        private static readonly Regex GstRegex = new(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", RegexOptions.IgnoreCase);

        private readonly Vendor? _editingVendor;

        /// <summary>The saved vendor, set only if the user clicked Submit successfully.</summary>
        public Vendor? SavedVendor { get; private set; }

        /// <summary>Pass null to add a new vendor, or an existing Vendor to edit it.</summary>
        public AddEditVendorWindow(Vendor? vendorToEdit = null)
        {
            InitializeComponent();
            _editingVendor = vendorToEdit;

            if (_editingVendor != null)
            {
                Title = "Edit Vendor";
                HeaderTitle.Text = "Edit Vendor";
                SubmitButton.Content = "Save Changes";

                NameBox.Text = _editingVendor.Name;
                GstBox.Text = _editingVendor.Gst;
                AddressBox.Text = _editingVendor.Address;
                ContactPersonBox.Text = _editingVendor.ContactPerson;
                ContactNumberBox.Text = _editingVendor.ContactNumber;
                EmailBox.Text = _editingVendor.Email;
                BankHolderBox.Text = _editingVendor.BankHolder;
                BankAccountBox.Text = _editingVendor.BankAccount;
                BankIfscBox.Text = _editingVendor.BankIfsc;
                BankBranchBox.Text = _editingVendor.BankBranch;
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter a vendor name before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameBox.Focus();
                return;
            }

            var gst = GstBox.Text.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(gst) && !GstRegex.IsMatch(gst))
            {
                MessageBox.Show(
                    "Invalid GST Number format.\n\nA valid Indian GSTIN must be 15 characters long (e.g. 27AAAAA0000A1Z5).\nFormat: 2-digit State Code + 10-char PAN + 1 entity code + 'Z' + 1 check digit.",
                    "Invalid GST Number",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                GstBox.Focus();
                return;
            }

            var contactNumber = ContactNumberBox.Text.Trim();
            if (!NumericInput.IsValidMobileNumber(contactNumber, isOptional: true))
            {
                MessageBox.Show(
                    "Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.",
                    "Invalid Contact Number",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ContactNumberBox.Focus();
                return;
            }

            var vendor = new Vendor
            {
                Id = _editingVendor?.Id,
                Name = name,
                Gst = gst,
                Address = AddressBox.Text.Trim(),
                ContactPerson = ContactPersonBox.Text.Trim(),
                ContactNumber = contactNumber,
                Email = EmailBox.Text.Trim(),
                BankHolder = BankHolderBox.Text.Trim(),
                BankAccount = BankAccountBox.Text.Trim(),
                BankIfsc = BankIfscBox.Text.Trim(),
                BankBranch = BankBranchBox.Text.Trim(),
            };

            SubmitButton.IsEnabled = false;
            try
            {
                var id = await VendorRepository.SaveVendorAsync(vendor);
                vendor.Id = id;
                SavedVendor = vendor;
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not save the vendor.\n\n{ex.Message}", "Error",
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
