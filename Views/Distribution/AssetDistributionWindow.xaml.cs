using StockPortalApp.Data;
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
    /// Interaction logic for AssetDistributionWindow.xaml
    /// </summary>
    public partial class AssetDistributionWindow : Window
    {
        private readonly string _itemName;

        public AssetDistributionWindow(string itemName)
        {
            InitializeComponent();
            _itemName = itemName;
            HeaderTitle.Text = itemName;
            Loaded += AssetDistributionWindow_Loaded;
        }

        private async void AssetDistributionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var records = await FixedAssetRepository.GetDistributionDetailAsync(_itemName);
                DetailGrid.ItemsSource = records;
                EmptyText.Visibility = records.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                DetailGrid.Visibility = records.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load distribution detail.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
