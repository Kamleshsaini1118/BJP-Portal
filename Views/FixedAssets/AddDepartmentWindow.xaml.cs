using StockPortalApp.Data;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace StockPortalApp.Views.FixedAssets
{
    public partial class AddDepartmentWindow : Window
    {
        private readonly List<Karyalya> _karyalyas;
        public Department? SavedDepartment { get; private set; }

        public AddDepartmentWindow(List<Karyalya> karyalyas, int? selectedKaryalyaId = null)
        {
            InitializeComponent();
            _karyalyas = karyalyas;
            KaryalyaCombo.ItemsSource = _karyalyas;

            if (selectedKaryalyaId.HasValue && selectedKaryalyaId.Value > 0)
            {
                KaryalyaCombo.SelectedItem = _karyalyas.FirstOrDefault(k => k.Id == selectedKaryalyaId.Value);
            }
            else
            {
                KaryalyaCombo.SelectedItem = null;
                KaryalyaCombo.SelectedIndex = -1;
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (KaryalyaCombo.SelectedItem is not Karyalya karyalya)
            {
                MessageBox.Show("Please select a Karyalya.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var deptName = DepartmentNameBox.Text.Trim();
            var inchargeName = InchargeNameBox.Text.Trim();
            var inchargeContact = InchargeContactBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(deptName))
            {
                MessageBox.Show("Please enter Department / Vibhag Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                DepartmentNameBox.Focus();
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

            var dept = new Department
            {
                KaryalyaId = karyalya.Id,
                KaryalyaName = karyalya.Name,
                Name = deptName,
                InchargeName = inchargeName,
                InchargeContact = inchargeContact,
                IsActive = true
            };

            SubmitButton.IsEnabled = false;
            try
            {
                int newId = await FixedAssetRepository.SaveDepartmentAsync(dept);
                dept.Id = newId;
                SavedDepartment = dept;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not add Department / Vibhag.\n\n{ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
