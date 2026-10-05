using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using StockPortalApp.Data;
using StockPortalApp.Models;

namespace StockPortalApp.Views.FixedAssets
{
    public partial class AssetHistoryWindow : Window
    {
        private readonly FixedAssetReportItem _item;

        public AssetHistoryWindow(FixedAssetReportItem item)
        {
            InitializeComponent();
            _item = item;

            HistoryHeaderTitle.Text = $"{item.ItemUniqueCode} · {item.ItemName}";
            HistoryHeaderSubtitle.Text = $"Located at {item.KaryalyaName} > {item.DepartmentName}, {item.RoomNoDisplay}";

            Loaded += AssetHistoryWindow_Loaded;
        }

        private async void AssetHistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var history = await FixedAssetRepository.GetItemHistoryAsync(_item);
                if (history != null && history.Count > 0)
                {
                    HistoryItemsList.ItemsSource = history;
                    NoHistoryText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    HistoryItemsList.ItemsSource = null;
                    NoHistoryText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load asset history.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
