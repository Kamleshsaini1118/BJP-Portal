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
    /// Interaction logic for VendorDetailsWindow.xaml
    /// </summary>
    public partial class VendorDetailsWindow : Window
    {
        public VendorDetailsWindow(Vendor vendor)
        {
            InitializeComponent();
            HeaderTitle.Text = vendor.Name;

            BuildVendorProfile(vendor);
        }

        private void BuildVendorProfile(Vendor vendor)
        {
            // Section 1: Business Details
            AddSectionHeader("BUSINESS & CONTACT INFORMATION");

            var grid1 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            grid1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var fieldGst = CreateFieldCard("GST NUMBER", vendor.Gst);
            Grid.SetColumn(fieldGst, 0);
            grid1.Children.Add(fieldGst);

            var fieldContactPerson = CreateFieldCard("CONTACT PERSON", vendor.ContactPerson);
            Grid.SetColumn(fieldContactPerson, 2);
            grid1.Children.Add(fieldContactPerson);

            DetailsPanel.Children.Add(grid1);

            var grid2 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var fieldPhone = CreateFieldCard("CONTACT NUMBER", vendor.ContactNumber);
            Grid.SetColumn(fieldPhone, 0);
            grid2.Children.Add(fieldPhone);

            var fieldEmail = CreateFieldCard("EMAIL ADDRESS", vendor.Email);
            Grid.SetColumn(fieldEmail, 2);
            grid2.Children.Add(fieldEmail);

            DetailsPanel.Children.Add(grid2);

            DetailsPanel.Children.Add(CreateFieldCard("BUSINESS ADDRESS", vendor.Address));

            // Section 2: Bank Details
            AddSectionHeader("BANK FINANCIAL DETAILS", isSecondary: true);

            var grid3 = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            grid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var fieldBankHolder = CreateFieldCard("ACCOUNT HOLDER", vendor.BankHolder);
            Grid.SetColumn(fieldBankHolder, 0);
            grid3.Children.Add(fieldBankHolder);

            var fieldAccount = CreateFieldCard("ACCOUNT NUMBER", vendor.BankAccount);
            Grid.SetColumn(fieldAccount, 2);
            grid3.Children.Add(fieldAccount);

            DetailsPanel.Children.Add(grid3);

            var grid4 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            grid4.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid4.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            grid4.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var fieldIfsc = CreateFieldCard("IFSC CODE", vendor.BankIfsc);
            Grid.SetColumn(fieldIfsc, 0);
            grid4.Children.Add(fieldIfsc);

            var fieldBranch = CreateFieldCard("BRANCH LOCATION", vendor.BankBranch);
            Grid.SetColumn(fieldBranch, 2);
            grid4.Children.Add(fieldBranch);

            DetailsPanel.Children.Add(grid4);
        }

        private void AddSectionHeader(string title, bool isSecondary = false)
        {
            if (isSecondary)
            {
                var divider = new Border
                {
                    Height = 1,
                    Background = (System.Windows.Media.Brush)FindResource("LineBrush"),
                    Margin = new Thickness(0, 12, 0, 14)
                };
                DetailsPanel.Children.Add(divider);
            }

            var header = new TextBlock
            {
                Text = title,
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("InkSoftBrush"),
                Margin = new Thickness(0, 0, 0, 12)
            };
            DetailsPanel.Children.Add(header);
        }

        private Border CreateFieldCard(string label, string? value)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xFC, 0xF9)),
                BorderBrush = (System.Windows.Media.Brush)FindResource("LineBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("InkSoftBrush"),
                Margin = new Thickness(0, 0, 0, 3)
            });

            var displayVal = string.IsNullOrWhiteSpace(value) ? "—" : value;
            stack.Children.Add(new TextBlock
            {
                Text = displayVal,
                FontSize = 13.5,
                FontWeight = string.IsNullOrWhiteSpace(value) ? FontWeights.Normal : FontWeights.SemiBold,
                Foreground = string.IsNullOrWhiteSpace(value)
                    ? (System.Windows.Media.Brush)FindResource("InkSoftBrush")
                    : (System.Windows.Media.Brush)FindResource("InkBrush"),
                TextWrapping = TextWrapping.Wrap
            });

            card.Child = stack;
            return card;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
