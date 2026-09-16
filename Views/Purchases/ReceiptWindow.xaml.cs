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
    /// Interaction logic for ReceiptWindow.xaml
    /// </summary>
    public partial class ReceiptWindow : Window
    {
        public ReceiptWindow(ReceiptData data)
        {
            InitializeComponent();
            Build(data);
        }

        private void Build(ReceiptData data)
        {
            ReceiptStack.Children.Clear();

            bool isReceived = data.Type == "received";
            bool isDamage = data.Type == "damage";

            string typeLabel = isDamage ? "DAMAGE REPORT" : (isReceived ? "GOODS RECEIVED" : "GOODS DISPATCHED");
            string personLabel = isDamage ? "REPORTED BY DETAILS" : (isReceived ? "RECEIVER DETAILS" : "RECIPIENT DETAILS");

            (string bgHex, string fgHex) badgeColors = isDamage
                ? ("#FDE8E4", "#B03A2E")
                : isReceived
                    ? ("#E8F5E4", "#0C6606")
                    : ("#FFF0DE", "#E2600A");

            // 1. Enterprise Top Letterhead Grid
            var topGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: Company Info
            var compStack = new StackPanel();
            compStack.Children.Add(new TextBlock
            {
                Text = "STOCK PORTAL APP",
                FontFamily = (FontFamily)FindResource("HeadingFont"),
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                Foreground = (Brush)FindResource("InkBrush")
            });
            compStack.Children.Add(new TextBlock
            {
                Text = data.Subtitle,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("SaffronDeepBrush"),
                Margin = new Thickness(0, 1, 0, 2)
            });
            compStack.Children.Add(new TextBlock
            {
                Text = "Official Logistics & Stock Management Record",
                FontSize = 10.5,
                Foreground = (Brush)FindResource("InkSoftBrush")
            });
            Grid.SetColumn(compStack, 0);
            topGrid.Children.Add(compStack);

            // Right: Receipt Info Box
            var receiptInfoBox = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var receiptInfoStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            receiptInfoStack.Children.Add(new TextBlock
            {
                Text = typeLabel,
                FontWeight = FontWeights.ExtraBold,
                FontSize = 13,
                Foreground = (Brush)new BrushConverter().ConvertFromString(badgeColors.fgHex)!,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            if (!string.IsNullOrWhiteSpace(data.BatchId))
            {
                receiptInfoStack.Children.Add(new TextBlock
                {
                    Text = $"{data.IdLabel}: {data.BatchId}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("InkBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
            if (!string.IsNullOrWhiteSpace(data.Date))
            {
                receiptInfoStack.Children.Add(new TextBlock
                {
                    Text = $"Date: {data.Date}",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("InkSoftBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
            }
            receiptInfoBox.Child = receiptInfoStack;
            Grid.SetColumn(receiptInfoBox, 1);
            topGrid.Children.Add(receiptInfoBox);

            ReceiptStack.Children.Add(topGrid);
            ReceiptStack.Children.Add(new Border { Height = 2, Background = (Brush)FindResource("SaffronDeepBrush"), Margin = new Thickness(0, 0, 0, 16) });

            // 2. Dual Meta Information Cards Grid
            var metaGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Card: Person Details
            var personCard = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var personStack = new StackPanel();
            personStack.Children.Add(new TextBlock { Text = personLabel, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 0, 0, 4) });
            personStack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(data.PersonName) ? "—" : data.PersonName, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkBrush") });
            if (!string.IsNullOrWhiteSpace(data.PersonNumber))
            {
                personStack.Children.Add(new TextBlock { Text = $"Phone: {data.PersonNumber}", FontSize = 11, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 2, 0, 0) });
            }
            personCard.Child = personStack;
            Grid.SetColumn(personCard, 0);
            metaGrid.Children.Add(personCard);

            // Right Card: Additional Record Details
            var recordCard = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var recordStack = new StackPanel();
            recordStack.Children.Add(new TextBlock { Text = "RECORD DETAILS", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 0, 0, 4) });

            if (data.ExtraMeta.Count > 0)
            {
                foreach (var extra in data.ExtraMeta)
                {
                    var valStr = string.IsNullOrWhiteSpace(extra.Value) ? "—" : extra.Value;
                    recordStack.Children.Add(new TextBlock { Text = $"{extra.Label}: {valStr}", FontSize = 11.5, Foreground = (Brush)FindResource("InkBrush"), Margin = new Thickness(0, 1, 0, 1) });
                }
            }
            else
            {
                recordStack.Children.Add(new TextBlock { Text = "Status: Logged System Entry", FontSize = 11, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 2, 0, 0) });
            }
            recordCard.Child = recordStack;
            Grid.SetColumn(recordCard, 2);
            metaGrid.Children.Add(recordCard);

            ReceiptStack.Children.Add(metaGrid);

            // 3. Items Table Header Bar & Rows
            bool hasFinancials = data.Items.Any(i => i.Price > 0 || i.Total > 0);

            if (hasFinancials)
            {
                // Dark Header Bar
                var tableHeaderBar = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#1E293B")!,
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Padding = new Thickness(10, 8, 10, 8)
                };
                var itemsHeader = new Grid();
                itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
                itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
                itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
                itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
                itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

                AddDarkHeaderCell(itemsHeader, "PRODUCT DETAILS", 0, TextAlignment.Left);
                AddDarkHeaderCell(itemsHeader, "QTY", 1, TextAlignment.Right);
                AddDarkHeaderCell(itemsHeader, "PRICE", 2, TextAlignment.Right);
                AddDarkHeaderCell(itemsHeader, "GST %", 3, TextAlignment.Right);
                AddDarkHeaderCell(itemsHeader, "AMOUNT (₹)", 4, TextAlignment.Right);
                tableHeaderBar.Child = itemsHeader;
                ReceiptStack.Children.Add(tableHeaderBar);

                decimal grandSubtotal = 0;
                decimal grandGst = 0;
                decimal grandTotal = 0;

                foreach (var item in data.Items)
                {
                    var subtotal = item.Qty * item.Price;
                    var gstAmt = subtotal * (item.GstPercent / 100m);
                    var totalVal = item.Total > 0 ? item.Total : (subtotal + gstAmt);

                    grandSubtotal += subtotal;
                    grandGst += gstAmt;
                    grandTotal += totalVal;

                    var row = new Grid { Margin = new Thickness(6, 6, 6, 6) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

                    var displayName = string.IsNullOrEmpty(item.Category) ? item.Name : $"{item.Name}\n[{item.Category}]";
                    var nameText = new TextBlock
                    {
                        Text = displayName,
                        FontSize = 12.5,
                        Foreground = (Brush)FindResource("InkBrush"),
                        TextWrapping = TextWrapping.Wrap
                    };
                    Grid.SetColumn(nameText, 0);
                    row.Children.Add(nameText);

                    AddBodyCell(row, item.Qty.ToString("N0"), 1, TextAlignment.Right);
                    AddBodyCell(row, $"₹{item.Price:N2}", 2, TextAlignment.Right);
                    AddBodyCell(row, $"{item.GstPercent:G29}%", 3, TextAlignment.Right);
                    AddBodyCell(row, $"₹{totalVal:N2}", 4, TextAlignment.Right, isBold: true);

                    ReceiptStack.Children.Add(row);
                    ReceiptStack.Children.Add(new Border { Height = 1, Background = (Brush)FindResource("LineBrush"), Margin = new Thickness(0, 2, 0, 2) });
                }

                // Summary Totals
                var summaryStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0), Width = 260 };
                summaryStack.Children.Add(CreateSummaryRow("Subtotal (Excl. Tax):", $"₹{grandSubtotal:N2}"));
                summaryStack.Children.Add(CreateSummaryRow("Total GST Tax:", $"₹{grandGst:N2}"));

                var grandTotalBorder = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#FFF5EC")!,
                    BorderBrush = (Brush)FindResource("SaffronDeepBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 6, 0, 0)
                };
                var gtGrid = new Grid();
                gtGrid.ColumnDefinitions.Add(new ColumnDefinition());
                gtGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                gtGrid.Children.Add(new TextBlock { Text = "GRAND TOTAL:", FontWeight = FontWeights.Bold, FontSize = 13.5, Foreground = (Brush)FindResource("InkBrush") });
                var gtVal = new TextBlock { Text = $"₹{grandTotal:N2}", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = (Brush)FindResource("SaffronDeepBrush") };
                Grid.SetColumn(gtVal, 1);
                gtGrid.Children.Add(gtVal);
                grandTotalBorder.Child = gtGrid;

                summaryStack.Children.Add(grandTotalBorder);
                ReceiptStack.Children.Add(summaryStack);
            }
            else
            {
                // Dark Header Bar
                var tableHeaderBar = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#1E293B")!,
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Padding = new Thickness(10, 8, 10, 8)
                };
                var itemsHeader = new Grid();
                itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
                itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                AddDarkHeaderCell(itemsHeader, "ITEM / PRODUCT DETAILS", 0, TextAlignment.Left);
                AddDarkHeaderCell(itemsHeader, "QUANTITY", 1, TextAlignment.Right);
                tableHeaderBar.Child = itemsHeader;
                ReceiptStack.Children.Add(tableHeaderBar);

                decimal total = 0;
                foreach (var item in data.Items)
                {
                    total += item.Qty;
                    var row = new Grid { Margin = new Thickness(6, 6, 6, 6) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var itemTitle = string.IsNullOrEmpty(item.Category) ? item.Name : $"{item.Name} [{item.Category}]";
                    row.Children.Add(new TextBlock { Text = itemTitle, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkBrush") });

                    var qtyText = new TextBlock { Text = item.Qty.ToString("N0"), FontSize = 13, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkBrush"), TextAlignment = TextAlignment.Right };
                    Grid.SetColumn(qtyText, 1);
                    row.Children.Add(qtyText);

                    ReceiptStack.Children.Add(row);
                    ReceiptStack.Children.Add(new Border { Height = 1, Background = (Brush)FindResource("LineBrush"), Margin = new Thickness(0, 2, 0, 2) });
                }

                var totalRow = new Grid { Margin = new Thickness(6, 8, 6, 4) };
                totalRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
                totalRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                totalRow.Children.Add(new TextBlock { Text = "TOTAL QUANTITY", FontWeight = FontWeights.Bold, FontSize = 13, Foreground = (Brush)FindResource("InkBrush") });

                var totalQtyText = new TextBlock { Text = total.ToString("N0"), FontWeight = FontWeights.Bold, FontSize = 13.5, Foreground = (Brush)FindResource("SaffronDeepBrush"), TextAlignment = TextAlignment.Right };
                Grid.SetColumn(totalQtyText, 1);
                totalRow.Children.Add(totalQtyText);
                ReceiptStack.Children.Add(totalRow);
            }

            // 4. Formal Signature Section
            var sigGrid = new Grid { Margin = new Thickness(0, 28, 0, 10) };
            sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30, GridUnitType.Pixel) });
            sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var sigLeft = new StackPanel();
            sigLeft.Children.Add(new Border { Height = 1, Background = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 0, 0, 4) });
            sigLeft.Children.Add(new TextBlock { Text = isDamage ? "Reported By Signature" : "Receiver Signature", FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkSoftBrush"), HorizontalAlignment = HorizontalAlignment.Center });
            Grid.SetColumn(sigLeft, 0);
            sigGrid.Children.Add(sigLeft);

            var sigRight = new StackPanel();
            sigRight.Children.Add(new Border { Height = 1, Background = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 0, 0, 4) });
            sigRight.Children.Add(new TextBlock { Text = "Authorized Store Manager", FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkSoftBrush"), HorizontalAlignment = HorizontalAlignment.Center });
            Grid.SetColumn(sigRight, 2);
            sigGrid.Children.Add(sigRight);

            ReceiptStack.Children.Add(sigGrid);

            // 5. Footnote
            ReceiptStack.Children.Add(new TextBlock
            {
                Text = "This is an official system-generated transaction receipt issued by Stock Portal.",
                FontSize = 10.5,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                Margin = new Thickness(0, 12, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        private void AddDarkHeaderCell(Grid grid, string text, int col, TextAlignment alignment)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = alignment
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void AddHeaderCell(Grid grid, string text, int col, TextAlignment alignment)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                TextAlignment = alignment
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void AddBodyCell(Grid grid, string text, int col, TextAlignment alignment, bool isBold = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (Brush)FindResource("InkBrush"),
                TextAlignment = alignment
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private UIElement CreateSummaryRow(string label, string value)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = (Brush)FindResource("InkSoftBrush") });
            var val = new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkBrush") };
            Grid.SetColumn(val, 1);
            g.Children.Add(val);
            return g;
        }

        private void AddMetaRow(string label, string? value)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = label, FontSize = 12.5, Foreground = (Brush)FindResource("InkSoftBrush") });
            var val = new TextBlock { Text = string.IsNullOrWhiteSpace(value) ? "—" : value, FontSize = 12.5, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkBrush") };
            Grid.SetColumn(val, 1);
            row.Children.Add(val);
            ReceiptStack.Children.Add(row);
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e) => Close();

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var capabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
                double pageWidth = capabilities.PageImageableArea?.ExtentWidth ?? printDialog.PrintableAreaWidth;
                double pageHeight = capabilities.PageImageableArea?.ExtentHeight ?? printDialog.PrintableAreaHeight;
                double originX = capabilities.PageImageableArea?.OriginWidth ?? 0;
                double originY = capabilities.PageImageableArea?.OriginHeight ?? 0;

                var printContainer = new Border
                {
                    Width = pageWidth,
                    Padding = new Thickness(Math.Max(16, originX), Math.Max(16, originY), Math.Max(16, originX), Math.Max(16, originY)),
                    Background = Brushes.White
                };

                var parent = (ScrollViewer)ReceiptContentPanel.Parent;
                parent.Content = null;

                ReceiptContentPanel.BorderThickness = new Thickness(0);
                printContainer.Child = ReceiptContentPanel;

                var pageSize = new Size(pageWidth, pageHeight);
                printContainer.Measure(pageSize);
                printContainer.Arrange(new Rect(new Point(0, 0), pageSize));
                printContainer.UpdateLayout();

                try
                {
                    printDialog.PrintVisual(printContainer, "Stock Portal Receipt");
                }
                finally
                {
                    printContainer.Child = null;
                    ReceiptContentPanel.BorderThickness = new Thickness(1);
                    parent.Content = ReceiptContentPanel;
                }
            }
        }
    }
}
