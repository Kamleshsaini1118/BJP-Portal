// using StockPortalApp.Data;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using System.Windows;
// using System.Windows.Controls;
// using System.Windows.Data;
// using System.Windows.Documents;
// using System.Windows.Input;
// using System.Windows.Media;
// using System.Windows.Media.Imaging;
// using System.Windows.Shapes;

// namespace StockPortalApp
// {
//     public partial class ItemLedgerWindow : Window
//     {
//         private readonly string _itemName;

//         public ItemLedgerWindow(string itemName)
//         {
//             InitializeComponent();
//             _itemName = itemName;
//             HeaderTitle.Text = itemName;
//             Loaded += ItemLedgerWindow_Loaded;
//         }

//         private async void ItemLedgerWindow_Loaded(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 var all = await FoodPacketRepository.GetLedgerAsync();
//                 var filtered = all.Where(x => x.Item == _itemName).ToList();

//                 LedgerGrid.ItemsSource = filtered;
//                 EmptyText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
//                 LedgerGrid.Visibility = filtered.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
//             }
//             catch (System.Exception ex)
//             {
//                 MessageBox.Show($"Could not load the ledger.\n\n{ex.Message}", "Error",
//                     MessageBoxButton.OK, MessageBoxImage.Error);
//             }
//         }

//         private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
//     }
// }


using System.Windows;
using StockPortalApp.Data;

namespace StockPortalApp
{
    public partial class ItemLedgerWindow : Window
    {
        private readonly string _itemName;

        public ItemLedgerWindow(string itemName)
        {
            InitializeComponent();
            _itemName = itemName;
            HeaderTitle.Text = itemName;
            Loaded += ItemLedgerWindow_Loaded;
        }

        private async void ItemLedgerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Full history across every source: batches, purchases, dispatches, damage, distributions.
                var entries = await StockReportRepository.GetProductLedgerAsync(_itemName);

                LedgerGrid.ItemsSource = entries;
                EmptyText.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                LedgerGrid.Visibility = entries.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Could not load the ledger.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}