using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using StockPortalApp.Data;
using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp.Views.FixedAssets
{
    public partial class MarkAssetDamagedWindow : Window
    {
        private readonly FixedAssetReportItem _item;
        private readonly ObservableCollection<DamageImageItem> _attachedImages = new();

        public MarkAssetDamagedWindow(FixedAssetReportItem item)
        {
            InitializeComponent();
            _item = item;

            AssetCodeText.Text = item.ItemUniqueCode;
            AssetNameText.Text = string.IsNullOrWhiteSpace(item.ProductCode)
                ? item.ItemName
                : $"{item.ItemName} ({item.ProductCode})";
            AssetLocationText.Text = $"{item.KaryalyaName} > {item.DepartmentName}, {item.RoomNoDisplay}";

            DamageDatePicker.SelectedDate = DateTime.Today;
            AttachedImagesControl.ItemsSource = _attachedImages;
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
                    if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) continue;
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
            if (sender is Button btn && btn.DataContext is DamageImageItem item)
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

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var name = ReporteeNameBox.Text.Trim();
            var number = ReporteeNumberBox.Text.Trim();
            var position = ReporteePositionBox.Text.Trim();
            var remark = RemarkBox.Text.Trim();
            var damageDate = DamageDatePicker.SelectedDate ?? DateTime.Today;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter Reportee Name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReporteeNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(number))
            {
                MessageBox.Show("Please enter Reportee Number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReporteeNumberBox.Focus();
                return;
            }

            if (!NumericInput.IsValidMobileNumber(number, isOptional: false))
            {
                MessageBox.Show("Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.", "Invalid Mobile Number", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReporteeNumberBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(position))
            {
                MessageBox.Show("Please enter Reportee Position.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReporteePositionBox.Focus();
                return;
            }

            SubmitButton.IsEnabled = false;
            try
            {
                string? combinedImages = _attachedImages.Count > 0
                    ? string.Join(";", _attachedImages.Select(i => i.FilePath))
                    : null;

                // 1. Mark status as Damaged and write to FixedAssetItemHistory
                await FixedAssetRepository.UpdateItemStatusAsync(_item, "Damaged");

                // 2. Add to DamageRecords so it appears in the Damage page!
                string itemLocationDesc = $"[{_item.ItemUniqueCode} - {_item.KaryalyaName} > {_item.DepartmentName}, {_item.RoomNoDisplay}]";
                string finalRemark = string.IsNullOrWhiteSpace(remark)
                    ? itemLocationDesc
                    : $"{itemLocationDesc} {remark}";

                await DamageRepository.AddRecordAsync(
                    category: "Fixed Asset",
                    itemName: $"{_item.ItemName} ({_item.ItemUniqueCode})",
                    qty: 1,
                    reporteeName: name,
                    reporteeNumber: number,
                    reporteePosition: position,
                    remark: finalRemark,
                    imagePath: combinedImages);

                MessageBox.Show("Asset marked as Damaged and added to Damage records successfully!", "Damage Reported", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not report asset damage.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
