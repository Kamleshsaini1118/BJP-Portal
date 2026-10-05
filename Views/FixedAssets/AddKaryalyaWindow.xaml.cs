using StockPortalApp.Data;
using StockPortalApp.Models;
using System;
using System.Windows;

namespace StockPortalApp.Views.FixedAssets
{
    public partial class AddKaryalyaWindow : Window
    {
        public Karyalya? SavedKaryalya { get; private set; }

        public AddKaryalyaWindow()
        {
            InitializeComponent();
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var name = KaryalyaNameBox.Text.Trim();
            var address = AddressBox.Text.Trim();
            var inchargeName = InchargeNameBox.Text.Trim();
            var inchargeContact = InchargeContactBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter Karyalya Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                KaryalyaNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Please enter Address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                AddressBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(inchargeName))
            {
                MessageBox.Show("Please enter Incharge Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                InchargeNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(inchargeContact))
            {
                MessageBox.Show("Please enter Incharge Contact Number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                InchargeContactBox.Focus();
                return;
            }

            var karyalya = new Karyalya
            {
                Name = name,
                Address = address,
                InchargeName = inchargeName,
                InchargeContact = inchargeContact,
                IsActive = true
            };

            SubmitButton.IsEnabled = false;
            try
            {
                int newId = await FixedAssetRepository.SaveKaryalyaAsync(karyalya);
                karyalya.Id = newId;
                SavedKaryalya = karyalya;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add Karyalya.\n\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
