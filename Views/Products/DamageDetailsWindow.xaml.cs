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
    /// Interaction logic for DamageDetailsWindow.xaml
    /// </summary>
    public partial class DamageDetailsWindow : Window
    {
        private readonly DamageRecord _record;

        public DamageDetailsWindow(DamageRecord record)
        {
            InitializeComponent();
            _record = record;
            SubtitleText.Text = $"{record.DamageCode} — {record.ItemName}";

            AddRow("Reportee Name", record.ReporteeName);
            AddRow("Reportee Number", record.ReporteeNumber);
            AddRow("Reportee Position", record.ReporteePosition);
            AddRow("Date", record.DateDisplay);
            AddRow("Remark", record.Remark);

            var imagePaths = record.GetImagePaths();
            var loadedImages = new List<ImageSource>();

            foreach (var path in imagePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        loadedImages.Add(bitmap);
                    }
                    catch { }
                }
            }

            if (loadedImages.Count > 0)
            {
                PhotosItemsControl.ItemsSource = loadedImages;
                PhotoPreviewPanel.Visibility = Visibility.Visible;
                NoPhotosPanel.Visibility = Visibility.Collapsed;
            }
            else if (imagePaths.Count > 0)
            {
                NoPhotosText.Text = "Attached photo file(s) not found on disk.";
            }
        }

        private void AddRow(string label, string? value)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = label, FontSize = 12.5, Foreground = (System.Windows.Media.Brush)FindResource("InkSoftBrush") });
            var val = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("InkBrush"),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 320
            };
            Grid.SetColumn(val, 1);
            row.Children.Add(val);
            MetaPanel.Children.Add(row);
        }

        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            var receipt = new ReceiptData
            {
                Type = "damage",
                BatchId = _record.DamageCode,
                IdLabel = "Damage ID",
                Subtitle = "Damage — Report Receipt",
                PersonName = _record.ReporteeName,
                PersonNumber = _record.ReporteeNumber,
                Items = new List<ReceiptItem> { new() { Name = _record.ItemName, Qty = _record.Qty, Category = _record.Category ?? "" } },
                Date = _record.DateDisplay,
                ExtraMeta = new List<ReceiptExtraField>
                {
                    new() { Label = "Reportee Position", Value = _record.ReporteePosition },
                    new() { Label = "Remark", Value = _record.Remark },
                }
            };

            var paths = _record.GetImagePaths();
            if (paths.Count > 0)
            {
                receipt.ExtraMeta.Add(new ReceiptExtraField { Label = "Attached Photos", Value = $"{paths.Count} photo(s) attached" });
            }

            new ReceiptWindow(receipt) { Owner = this }.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
