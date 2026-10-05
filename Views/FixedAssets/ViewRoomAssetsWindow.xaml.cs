using StockPortalApp.Data;
using StockPortalApp.Models;
using System;
using System.Windows;

namespace StockPortalApp.Views.FixedAssets
{
    public partial class ViewRoomAssetsWindow : Window
    {
        public ViewRoomAssetsWindow(FixedAssetRoom room)
        {
            InitializeComponent();

            RoomTitleText.Text = $"{room.Karyalya} — {room.Department}";
            RoomSubtitleText.Text = $"Room: {room.RoomNoDisplay} | Total Mapped Items: {room.TotalAssets}";
            Loaded += async (s, e) =>
            {
                try
                {
                    var items = await FixedAssetRepository.GetRoomItemsAsync(room.RoomId);
                    ItemsGrid.ItemsSource = items;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not load items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
