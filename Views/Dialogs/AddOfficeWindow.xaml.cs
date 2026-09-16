using StockPortalApp.Data;
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
    /// Interaction logic for AddOfficeWindow.xaml
    /// </summary>
    public partial class AddOfficeWindow : Window
    {
        public DistributionOffice? SavedOffice { get; private set; }

        public AddOfficeWindow()
        {
            InitializeComponent();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter an office or destination name before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SubmitButton.IsEnabled = false;
            try
            {
                var address = AddressBox.Text.Trim();
                var contactPerson = ContactPersonBox.Text.Trim();
                var contactNumber = ContactNumberBox.Text.Trim();

                SavedOffice = await OfficeRepository.AddOfficeAsync(
                    name,
                    string.IsNullOrWhiteSpace(address) ? null : address,
                    string.IsNullOrWhiteSpace(contactPerson) ? null : contactPerson,
                    string.IsNullOrWhiteSpace(contactNumber) ? null : contactNumber);

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not save the office.\n\n{ex.Message}", "Error",
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
