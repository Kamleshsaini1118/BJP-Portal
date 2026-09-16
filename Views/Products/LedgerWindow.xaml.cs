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
    /// Interaction logic for LedgerWindow.xaml
    /// </summary>
    public partial class LedgerWindow : Window
    {
        public LedgerWindow()
        {
            InitializeComponent();
            Loaded += LedgerWindow_Loaded;
        }

        private async void LedgerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var entries = await FoodPacketRepository.GetLedgerAsync();
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
