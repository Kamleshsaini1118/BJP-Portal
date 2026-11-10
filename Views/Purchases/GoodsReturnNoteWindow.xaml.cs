using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StockPortalApp.Models;

namespace StockPortalApp.Views.Purchases
{
    public partial class GoodsReturnNoteWindow : Window
    {
        public GoodsReturnNoteWindow(GoodsReturn header, List<GoodsReturnItem> items)
        {
            InitializeComponent();
            Build(header, items);
        }

        private void Build(GoodsReturn header, List<GoodsReturnItem> items)
        {
            ContentStack.Children.Clear();

            // 1. Top Letterhead
            var topGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var compStack = new StackPanel();
            compStack.Children.Add(new TextBlock
            {
                Text = "BJP STATE OFFICE APPLICATION",
                FontFamily = (FontFamily)FindResource("HeadingFont"),
                FontWeight = FontWeights.Bold,
                FontSize = 17,
                Foreground = (Brush)FindResource("InkBrush")
            });
            compStack.Children.Add(new TextBlock
            {
                Text = "Stock Portal — Official Goods Return Note",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("SaffronDeepBrush"),
                Margin = new Thickness(0, 2, 0, 2)
            });
            compStack.Children.Add(new TextBlock
            {
                Text = $"Reason: {header.Reason}",
                FontSize = 11,
                Foreground = (Brush)FindResource("InkSoftBrush")
            });
            Grid.SetColumn(compStack, 0);
            topGrid.Children.Add(compStack);

            var infoBox = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var infoStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            infoStack.Children.Add(new TextBlock
            {
                Text = "GOODS RETURN NOTE",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 12.5,
                Foreground = (Brush)FindResource("SaffronDeepBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Return No: {header.ReturnNo}",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = (Brush)FindResource("InkBrush"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 3, 0, 2)
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Return Date: {header.ReturnDateDisplay}",
                FontSize = 11,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            infoBox.Child = infoStack;
            Grid.SetColumn(infoBox, 1);
            topGrid.Children.Add(infoBox);

            ContentStack.Children.Add(topGrid);
            ContentStack.Children.Add(new Border
            {
                Height = 2,
                Background = (Brush)FindResource("SaffronDeepBrush"),
                Margin = new Thickness(0, 0, 0, 16)
            });

            // 2. Vendor & Reference Cards
            var metaGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var vendorCard = CreateMetaCard("RETURNED TO VENDOR", cardStack =>
            {
                cardStack.Children.Add(new TextBlock
                {
                    Text = header.VendorName,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("InkBrush"),
                    TextWrapping = TextWrapping.Wrap
                });
                if (!string.IsNullOrWhiteSpace(header.VendorGst))
                {
                    cardStack.Children.Add(new TextBlock
                    {
                        Text = $"GSTIN: {header.VendorGst}",
                        FontSize = 11,
                        Foreground = (Brush)FindResource("InkSoftBrush"),
                        Margin = new Thickness(0, 3, 0, 0)
                    });
                }
                if (!string.IsNullOrWhiteSpace(header.VendorAddress))
                {
                    cardStack.Children.Add(new TextBlock
                    {
                        Text = header.VendorAddress,
                        FontSize = 11,
                        Foreground = (Brush)FindResource("InkSoftBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
                if (!string.IsNullOrWhiteSpace(header.VendorContactPerson) || !string.IsNullOrWhiteSpace(header.VendorContactNumber))
                {
                    string contact = string.Join(" · ",
                        new[] { header.VendorContactPerson, header.VendorContactNumber }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                    cardStack.Children.Add(new TextBlock
                    {
                        Text = contact,
                        FontSize = 11,
                        Foreground = (Brush)FindResource("InkSoftBrush"),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
            });
            Grid.SetColumn(vendorCard, 0);
            metaGrid.Children.Add(vendorCard);

            var refCard = CreateMetaCard("PURCHASE & HANDOVER DETAILS", cardStack =>
            {
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"Against Purchase: {header.AgainstPurchaseRef}",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("InkBrush")
                });
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"Purchase Date: {header.PurchaseDateDisplay}",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("InkSoftBrush"),
                    Margin = new Thickness(0, 3, 0, 0)
                });
                if (!string.IsNullOrWhiteSpace(header.PoReference))
                {
                    cardStack.Children.Add(new TextBlock
                    {
                        Text = $"PO Reference: {header.PoReference}",
                        FontSize = 11,
                        Foreground = (Brush)FindResource("InkSoftBrush"),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"Handed Over To: {header.HandedOverToDisplay}",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("InkSoftBrush"),
                    Margin = new Thickness(0, 4, 0, 0)
                });
                if (!string.IsNullOrWhiteSpace(header.ContactNumber))
                {
                    cardStack.Children.Add(new TextBlock
                    {
                        Text = $"Contact No: {header.ContactNumber}",
                        FontSize = 11,
                        Foreground = (Brush)FindResource("InkSoftBrush"),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
            });
            Grid.SetColumn(refCard, 2);
            metaGrid.Children.Add(refCard);

            ContentStack.Children.Add(metaGrid);

            // 3. Items Table
            var tableBorder = new Border
            {
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 14),
                ClipToBounds = true
            };
            var tableStack = new StackPanel();

            var hdrBorder = new Border
            {
                Background = (Brush)FindResource("InkBrush"),
                Padding = new Thickness(10, 8, 10, 8)
            };
            var hdrGrid = CreateItemRowGrid();
            AddHeaderCell(hdrGrid, "#", 0, TextAlignment.Left);
            AddHeaderCell(hdrGrid, "ITEM / DESCRIPTION", 1, TextAlignment.Left);
            AddHeaderCell(hdrGrid, "CATEGORY", 2, TextAlignment.Left);
            AddHeaderCell(hdrGrid, "QTY", 3, TextAlignment.Right);
            AddHeaderCell(hdrGrid, "RATE", 4, TextAlignment.Right);
            AddHeaderCell(hdrGrid, "GST", 5, TextAlignment.Right);
            AddHeaderCell(hdrGrid, "AMOUNT", 6, TextAlignment.Right);
            hdrBorder.Child = hdrGrid;
            tableStack.Children.Add(hdrBorder);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var rowBorder = new Border
                {
                    Background = i % 2 == 1
                        ? (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!
                        : Brushes.White,
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                    BorderThickness = new Thickness(0, 0, 0, i == items.Count - 1 ? 0 : 1),
                    Padding = new Thickness(10, 8, 10, 8)
                };
                var rowGrid = CreateItemRowGrid();

                AddBodyCell(rowGrid, (i + 1).ToString(), 0, TextAlignment.Left);

                var itemNameText = string.IsNullOrWhiteSpace(item.SerialNumber)
                    ? item.ProductName
                    : $"{item.ProductName}\nS/N: {item.SerialNumber}";
                AddBodyCell(rowGrid, itemNameText, 1, TextAlignment.Left, isBold: true);
                AddBodyCell(rowGrid, item.CategoryDisplay, 2, TextAlignment.Left);
                AddBodyCell(rowGrid, item.ReturnQtyDisplay, 3, TextAlignment.Right, isBold: true);
                AddBodyCell(rowGrid, item.RateDisplay, 4, TextAlignment.Right);
                AddBodyCell(rowGrid, item.GstDisplay, 5, TextAlignment.Right);
                AddBodyCell(rowGrid, item.AmountDisplay, 6, TextAlignment.Right, isBold: true);

                rowBorder.Child = rowGrid;
                tableStack.Children.Add(rowBorder);
            }

            tableBorder.Child = tableStack;
            ContentStack.Children.Add(tableBorder);

            // 4. Summary & Remarks
            var bottomGrid = new Grid { Margin = new Thickness(0, 4, 0, 12) };
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20, GridUnitType.Pixel) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var remarkBox = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#FFFBF7")!,
                BorderBrush = (Brush)FindResource("LineBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var remarkStack = new StackPanel();
            remarkStack.Children.Add(new TextBlock
            {
                Text = "REMARKS / NOTES",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                Margin = new Thickness(0, 0, 0, 4)
            });
            remarkStack.Children.Add(new TextBlock
            {
                Text = header.RemarksDisplay,
                FontSize = 11.5,
                Foreground = (Brush)FindResource("InkBrush"),
                TextWrapping = TextWrapping.Wrap
            });
            remarkBox.Child = remarkStack;
            Grid.SetColumn(remarkBox, 0);
            bottomGrid.Children.Add(remarkBox);

            decimal totalQty = items.Sum(x => x.ReturnQty);
            decimal subTotal = items.Sum(x => Math.Round(x.ReturnQty * x.Rate, 2));
            decimal totalGst = items.Sum(x => x.Amount) - subTotal;
            decimal grandTotal = items.Sum(x => x.Amount);

            var totalBox = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var totalStack = new StackPanel();
            totalStack.Children.Add(CreateSummaryRow("Total Return Qty", totalQty % 1 == 0 ? totalQty.ToString("0") : totalQty.ToString("0.##")));
            totalStack.Children.Add(CreateSummaryRow("Taxable Value", $"₹{subTotal:N2}"));
            totalStack.Children.Add(CreateSummaryRow("GST Amount", $"₹{totalGst:N2}"));
            totalStack.Children.Add(new Border
            {
                Height = 1,
                Background = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!,
                Margin = new Thickness(0, 6, 0, 6)
            });
            var grandRow = new Grid();
            grandRow.ColumnDefinitions.Add(new ColumnDefinition());
            grandRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grandRow.Children.Add(new TextBlock
            {
                Text = "TOTAL RETURN VALUE",
                FontSize = 11.5,
                FontWeight = FontWeights.ExtraBold,
                Foreground = (Brush)FindResource("InkBrush")
            });
            var grandVal = new TextBlock
            {
                Text = $"₹{grandTotal:N2}",
                FontSize = 13.5,
                FontWeight = FontWeights.ExtraBold,
                Foreground = (Brush)FindResource("SaffronDeepBrush")
            };
            Grid.SetColumn(grandVal, 1);
            grandRow.Children.Add(grandVal);
            totalStack.Children.Add(grandRow);

            totalBox.Child = totalStack;
            Grid.SetColumn(totalBox, 2);
            bottomGrid.Children.Add(totalBox);

            ContentStack.Children.Add(bottomGrid);

            // 5. Signatures
            var sigGrid = new Grid { Margin = new Thickness(0, 32, 0, 0) };
            sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftSig = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            leftSig.Children.Add(new Border { Height = 1, Width = 170, Background = (Brush)FindResource("LineBrush"), Margin = new Thickness(0, 0, 0, 4) });
            leftSig.Children.Add(new TextBlock { Text = "Dispatched / Returned By", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkSoftBrush") });
            Grid.SetColumn(leftSig, 0);
            sigGrid.Children.Add(leftSig);

            var rightSig = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            rightSig.Children.Add(new Border { Height = 1, Width = 170, Background = (Brush)FindResource("LineBrush"), Margin = new Thickness(0, 0, 0, 4) });
            rightSig.Children.Add(new TextBlock { Text = "Received By Vendor / Representative", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkSoftBrush"), HorizontalAlignment = HorizontalAlignment.Right });
            Grid.SetColumn(rightSig, 1);
            sigGrid.Children.Add(rightSig);

            ContentStack.Children.Add(sigGrid);

            ContentStack.Children.Add(new TextBlock
            {
                Text = "This is an official system-generated Goods Return Note from Stock Portal.",
                FontSize = 10,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                Margin = new Thickness(0, 22, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        private Border CreateMetaCard(string title, Action<StackPanel> populate)
        {
            var card = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                Margin = new Thickness(0, 0, 0, 4)
            });
            populate(stack);
            card.Child = stack;
            return card;
        }

        private static Grid CreateItemRowGrid()
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.65, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            return g;
        }

        private static void AddHeaderCell(Grid grid, string text, int col, TextAlignment alignment)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void AddBodyCell(Grid grid, string text, int col, TextAlignment alignment, bool isBold = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (Brush)FindResource("InkBrush"),
                TextAlignment = alignment,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private UIElement CreateSummaryRow(string label, string value)
        {
            var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.Children.Add(new TextBlock { Text = label, FontSize = 11.5, Foreground = (Brush)FindResource("InkSoftBrush") });
            var val = new TextBlock { Text = value, FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkBrush") };
            Grid.SetColumn(val, 1);
            g.Children.Add(val);
            return g;
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e) => Close();

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                double pageWidth = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 794;
                double pageHeight = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 1123;

                NoteScrollViewer.Content = null;

                var printWrapper = new Border
                {
                    Width = pageWidth,
                    Height = pageHeight,
                    Background = Brushes.White,
                    Padding = new Thickness(35, 35, 35, 35)
                };

                PrintContentPanel.Width = double.NaN;
                PrintContentPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                PrintContentPanel.BorderThickness = new Thickness(0);
                printWrapper.Child = PrintContentPanel;

                var pSize = new Size(pageWidth, pageHeight);
                printWrapper.Measure(pSize);
                printWrapper.Arrange(new Rect(new Point(0, 0), pSize));
                printWrapper.UpdateLayout();

                try
                {
                    printDialog.PrintVisual(printWrapper, "Goods Return Note");
                }
                finally
                {
                    printWrapper.Child = null;
                    PrintContentPanel.Width = 700;
                    PrintContentPanel.BorderThickness = new Thickness(1);
                    PrintContentPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    NoteScrollViewer.Content = PrintContentPanel;
                }
            }
        }
    }
}
