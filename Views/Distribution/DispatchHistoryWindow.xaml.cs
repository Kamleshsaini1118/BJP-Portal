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
    /// Interaction logic for DispatchHistoryWindow.xaml
    /// </summary>
    public partial class DispatchHistoryWindow : Window
    {
        private readonly FoodPacketBatch _batch;

        public DispatchHistoryWindow(FoodPacketBatch batch)
        {
            InitializeComponent();
            _batch = batch;
            HeaderTitle.Text = $"Dispatch History — {batch.BatchCode}";
            Loaded += DispatchHistoryWindow_Loaded;
        }

        private async void DispatchHistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var records = await FoodPacketRepository.GetDispatchesAsync(_batch.Id);
                HistoryGrid.ItemsSource = records;
                EmptyText.Visibility = records.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                HistoryGrid.Visibility = records.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load dispatch history.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: BatchDispatchRecord record }) return;

            var receipt = new ReceiptData
            {
                Type = "dispatched",
                BatchId = _batch.BatchCode,
                PersonName = record.PersonName,
                PersonNumber = record.PersonNumber,
                Items = new List<ReceiptItem> { new() { Name = _batch.ItemName, Qty = record.Qty } },
                Date = record.DispatchDate.ToString("dd MMM yyyy")
            };

            new ReceiptWindow(receipt) { Owner = this }.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
