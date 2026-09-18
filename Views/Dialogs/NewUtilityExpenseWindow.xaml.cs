using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp
{
    public partial class NewUtilityExpenseWindow : Window
    {
        private string? _selectedImagePath;
        private string? _copiedImagePath;

        public UtilityExpense? SavedExpense { get; private set; }

        public NewUtilityExpenseWindow()
        {
            InitializeComponent();

            CategoryCombo.ItemsSource = new[]
            {
                "Plumbing",
                "Electrician",
                "Carpentry",
                "Maintenance",
                "AC Service",
                "Other"
            };
            CategoryCombo.SelectedIndex = 0;

            ExpenseDatePicker.SelectedDate = IndiaTime.Today;
        }

        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Bill or Receipt Image",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _selectedImagePath = openFileDialog.FileName;
                    var fileInfo = new FileInfo(_selectedImagePath);

                    BillFileNameText.Text = fileInfo.Name;
                    BillFileSubtext.Text = $"{fileInfo.Length / 1024:N0} KB — Selected File";

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    BillImagePreview.Source = bitmap;
                    BillImagePreview.Visibility = Visibility.Visible;
                    DefaultFileIcon.Visibility = Visibility.Collapsed;
                    RemoveImageButton.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not load the selected image.\n\n{ex.Message}", "Image Load Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedImagePath = null;
            _copiedImagePath = null;
            BillImagePreview.Source = null;
            BillImagePreview.Visibility = Visibility.Collapsed;
            DefaultFileIcon.Visibility = Visibility.Visible;
            RemoveImageButton.Visibility = Visibility.Collapsed;
            BillFileNameText.Text = "No file attached";
            BillFileSubtext.Text = "Attach a PNG, JPG or JPEG bill photo";
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string category = CategoryCombo.SelectedItem as string ?? "Plumbing";
            string location = LocationBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(location))
            {
                MessageBox.Show("Please enter the location of the utility expense.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LocationBox.Focus();
                return;
            }

            decimal amount = 0;
            if (!string.IsNullOrWhiteSpace(AmountBox.Text))
            {
                if (!decimal.TryParse(AmountBox.Text.Trim(), out amount) || amount < 0)
                {
                    MessageBox.Show("Please enter a valid expense amount.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    AmountBox.Focus();
                    return;
                }
            }

            DateTime expenseDate = ExpenseDatePicker.SelectedDate ?? IndiaTime.Today;
            string? description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();
            string? billNo = string.IsNullOrWhiteSpace(BillNoBox.Text) ? null : BillNoBox.Text.Trim();

            // Handle copying image file locally if provided
            if (!string.IsNullOrWhiteSpace(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                try
                {
                    string appDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppStorage", "UtilityBills");
                    if (!Directory.Exists(appDataFolder))
                    {
                        Directory.CreateDirectory(appDataFolder);
                    }

                    string extension = Path.GetExtension(_selectedImagePath);
                    string destFileName = $"bill_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}{extension}";
                    _copiedImagePath = Path.Combine(appDataFolder, destFileName);

                    File.Copy(_selectedImagePath, _copiedImagePath, overwrite: true);
                }
                catch
                {
                    // Fallback to original image path if copy fails
                    _copiedImagePath = _selectedImagePath;
                }
            }

            SubmitButton.IsEnabled = false;

            try
            {
                var expense = new UtilityExpense
                {
                    Category = category,
                    Location = location,
                    Amount = amount,
                    ExpenseDate = expenseDate,
                    Description = description,
                    BillNo = billNo,
                    BillImagePath = _copiedImagePath,
                    CreatedAt = DateTime.Now
                };

                await UtilityExpenseRepository.SaveExpenseAsync(expense);
                SavedExpense = expense;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save utility expense.\n\n{ex.Message}", "Error",
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
