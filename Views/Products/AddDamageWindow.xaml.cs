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
    /// Interaction logic for AddDamageWindow.xaml
    /// </summary>
    public partial class AddDamageWindow : Window
    {
        private readonly List<ProductCategory> _categories;
        private readonly List<Product> _products;
        private readonly ObservableCollection<DamageImageItem> _attachedImages = new();
        private List<StockReportItem> _stockReport = new();

        /// <summary>Set when Submit succeeds, so the caller can show a receipt for what was created.</summary>
        public ReceiptData? CreatedReceipt { get; private set; }

        public AddDamageWindow(List<ProductCategory> categories, List<Product> products)
        {
            InitializeComponent();
            _categories = new List<ProductCategory>(categories);
            _products = new List<Product>(products);

            AttachedImagesControl.ItemsSource = _attachedImages;

            RebuildCategoryCombo();
            RebuildItemCombo();

            Loaded += async (s, e) =>
            {
                await LoadStockReportAsync();
            };
        }

        private async System.Threading.Tasks.Task LoadStockReportAsync()
        {
            try
            {
                _stockReport = await StockReportRepository.GetReportAsync(null, null);
                UpdateAvailableStockDisplay();
            }
            catch { }
        }

        private decimal GetAvailableStockForItem(string? itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName) || itemName == "+ Add New Item") return 0;
            var match = _stockReport.FirstOrDefault(s => string.Equals(s.Item?.Trim(), itemName.Trim(), StringComparison.OrdinalIgnoreCase));
            return match?.Remaining ?? 0;
        }

        private void UpdateAvailableStockDisplay()
        {
            var selectedItem = ItemCombo.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedItem) && selectedItem != "+ Add New Item")
            {
                var avail = GetAvailableStockForItem(selectedItem);
                AvailableStockText.Text = $"(Available: {avail:N0})";
                AvailableStockText.Foreground = avail > 0
                    ? (Brush)FindResource("InkSoftBrush")
                    : (Brush)new BrushConverter().ConvertFromString("#B03A2E")!;
            }
            else
            {
                AvailableStockText.Text = "";
            }
        }

        private void RebuildCategoryCombo()
        {
            var catNames = _categories.Select(c => c.Name).ToList();
            if (!catNames.Contains("+ Add New Category"))
            {
                catNames.Add("+ Add New Category");
            }
            CategoryCombo.ItemsSource = catNames;
        }

        private void RebuildItemCombo()
        {
            var category = CategoryCombo.SelectedItem as string;
            var itemNames = string.IsNullOrEmpty(category) || category == "+ Add New Category"
                ? _products.Select(p => p.Name).ToList()
                : _products.Where(p => p.Category == category).Select(p => p.Name).ToList();

            if (!itemNames.Contains("+ Add New Item"))
            {
                itemNames.Add("+ Add New Item");
            }
            ItemCombo.ItemsSource = itemNames;
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var category = CategoryCombo.SelectedItem as string;
            if (category == "+ Add New Category")
            {
                OpenAddProductModal();
            }
            else
            {
                RebuildItemCombo();
                UpdateAvailableStockDisplay();
            }
        }

        private void ItemCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ItemCombo.SelectedItem as string;
            if (item == "+ Add New Item")
            {
                OpenAddProductModal();
            }
            else
            {
                UpdateAvailableStockDisplay();
            }
        }

        private async void OpenAddProductModal()
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

                await LoadStockReportAsync();

                RebuildCategoryCombo();
                CategoryCombo.SelectedItem = p.Category;

                RebuildItemCombo();
                ItemCombo.SelectedItem = p.Name;
            }
            else
            {
                CategoryCombo.SelectedItem = null;
                ItemCombo.SelectedItem = null;
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var category = CategoryCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Select a category before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = ItemCombo.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(item))
            {
                MessageBox.Show("Select an item before saving.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var qty = decimal.TryParse(QtyBox.Text.Trim(), out var q) ? q : 0;
            if (qty <= 0)
            {
                MessageBox.Show("Enter a quantity greater than zero.", "Missing information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var availableStock = GetAvailableStockForItem(item);
            if (qty > availableStock)
            {
                MessageBox.Show(
                    $"Entered damage quantity ({qty:N0}) exceeds the actual available product stock ({availableStock:N0}).\n\nPlease enter a quantity less than or equal to {availableStock:N0}.",
                    "Exceeds Available Stock",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QtyBox.Focus();
                return;
            }

            var reporteeName = ReporteeNameBox.Text.Trim();
            var reporteeNumber = ReporteeNumberBox.Text.Trim();
            var reporteePosition = ReporteePositionBox.Text.Trim();
            var remark = RemarkBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(reporteeNumber) && !NumericInput.IsValidMobileNumber(reporteeNumber, isOptional: true))
            {
                MessageBox.Show(
                    "Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.",
                    "Invalid Mobile Number",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ReporteeNumberBox.Focus();
                return;
            }

            SubmitButton.IsEnabled = false;
            try
            {
                string? combinedImagePath = _attachedImages.Count > 0
                    ? string.Join(";", _attachedImages.Select(i => i.FilePath))
                    : null;

                var (_, code) = await DamageRepository.AddRecordAsync(
                    category, item, qty, reporteeName, reporteeNumber, reporteePosition, remark, combinedImagePath);

                CreatedReceipt = new ReceiptData
                {
                    Type = "damage",
                    BatchId = code,
                    IdLabel = "Damage ID",
                    Subtitle = "Damage — Report Receipt",
                    PersonName = reporteeName,
                    PersonNumber = reporteeNumber,
                    Items = new List<ReceiptItem> { new() { Name = item, Qty = qty, Category = category ?? "" } },
                    Date = IndiaTime.Today.ToString("dd MMM yyyy"),
                    ExtraMeta = new List<ReceiptExtraField>
                    {
                        new() { Label = "Reportee Position", Value = reporteePosition },
                        new() { Label = "Remark", Value = remark },
                    }
                };

                if (_attachedImages.Count > 0)
                {
                    CreatedReceipt.ExtraMeta.Add(new ReceiptExtraField
                    {
                        Label = "Attached Photos",
                        Value = $"{_attachedImages.Count} photo(s) attached"
                    });
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the damage report.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitButton.IsEnabled = true;
            }
        }

        private void AttachImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Damage Photos",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filePath in openFileDialog.FileNames)
                {
                    if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath)) continue;
                    if (_attachedImages.Any(img => string.Equals(img.FilePath, filePath, StringComparison.OrdinalIgnoreCase))) continue;

                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();

                        _attachedImages.Add(new DamageImageItem
                        {
                            FilePath = filePath,
                            PreviewImage = bitmap
                        });
                    }
                    catch { }
                }

                UpdateImageSectionState();
            }
        }

        private void RemoveSingleImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: DamageImageItem item })
            {
                _attachedImages.Remove(item);
                UpdateImageSectionState();
            }
        }

        private void RemoveAllImages_Click(object sender, RoutedEventArgs e)
        {
            _attachedImages.Clear();
            UpdateImageSectionState();
        }

        private void UpdateImageSectionState()
        {
            if (_attachedImages.Count > 0)
            {
                NoImageBorder.Visibility = Visibility.Collapsed;
                ImagePreviewBorder.Visibility = Visibility.Visible;
                AttachedCountText.Text = $"Attached Photos ({_attachedImages.Count})";
            }
            else
            {
                NoImageBorder.Visibility = Visibility.Visible;
                ImagePreviewBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class DamageImageItem
    {
        public string FilePath { get; set; } = "";
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public ImageSource? PreviewImage { get; set; }
    }
}
