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
    /// Interaction logic for PurchaseReceiptWindow.xaml
    /// </summary>
    public partial class PurchaseReceiptWindow : Window
    {
        public PurchaseReceiptWindow(
            Purchase purchase, List<PurchaseOrderItemRow> purchaseItems,
            (string? ShipmentId, string? Destination, DateTime? Date, string? ReceiverName, string? ReceiverNumber, string? Position, string? Remark, List<(string? Category, string ProductName, decimal Qty)> Items)? distribution)
        {
            InitializeComponent();
            Build(purchase, purchaseItems, distribution);
        }

        private void Build(
            Purchase purchase, List<PurchaseOrderItemRow> purchaseItems,
            (string? ShipmentId, string? Destination, DateTime? Date, string? ReceiverName, string? ReceiverNumber, string? Position, string? Remark, List<(string? Category, string ProductName, decimal Qty)> Items)? distribution)
        {
            ContentStack.Children.Clear();

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
                Text = "Procurement — Official Purchase Receipt",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("SaffronDeepBrush"),
                Margin = new Thickness(0, 1, 0, 2)
            });
            compStack.Children.Add(new TextBlock
            {
                Text = "GSTIN: 27AAACS1429B1Z2 | CIN: U74999MH2024PTC123456",
                FontSize = 10.5,
                Foreground = (Brush)FindResource("InkSoftBrush")
            });
            compStack.Children.Add(new TextBlock
            {
                Text = "Central Logistics Park, Sector 7, MH - 411001",
                FontSize = 10.5,
                Foreground = (Brush)FindResource("InkSoftBrush")
            });
            Grid.SetColumn(compStack, 0);
            topGrid.Children.Add(compStack);

            // Right: Receipt Type Badge & Reference Details
            var badgeColor = purchase.PurchaseType == "Direct" ? "#FFF0DE" : "#E8F5E4";
            var badgeFg = purchase.PurchaseType == "Direct" ? "#E2600A" : "#0C6606";

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
                Text = $"PURCHASE — {purchase.PurchaseType.ToUpper()}",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 13,
                Foreground = (Brush)new BrushConverter().ConvertFromString(badgeFg)!,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            if (!string.IsNullOrWhiteSpace(purchase.PoReference))
            {
                receiptInfoStack.Children.Add(new TextBlock
                {
                    Text = $"Ref: {purchase.PoReference}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("InkBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }
            receiptInfoStack.Children.Add(new TextBlock
            {
                Text = $"Invoice No: {purchase.InvoiceNumberDisplay}",
                FontSize = 11,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            receiptInfoStack.Children.Add(new TextBlock
            {
                Text = $"Date: {purchase.PurchaseDateDisplay}",
                FontSize = 11,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            receiptInfoBox.Child = receiptInfoStack;
            Grid.SetColumn(receiptInfoBox, 1);
            topGrid.Children.Add(receiptInfoBox);

            ContentStack.Children.Add(topGrid);
            ContentStack.Children.Add(new Border { Height = 2, Background = (Brush)FindResource("SaffronDeepBrush"), Margin = new Thickness(0, 0, 0, 16) });

            // 2. Vendor & Receiver Details Cards
            var metaGrid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
            metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Vendor Card
            var vendorCard = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var vendorStack = new StackPanel();
            vendorStack.Children.Add(new TextBlock { Text = "VENDOR / SUPPLIER", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 0, 0, 4) });
            vendorStack.Children.Add(new TextBlock { Text = purchase.VendorName, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkBrush") });
            vendorStack.Children.Add(new TextBlock { Text = $"Payment Mode: {purchase.PaymentModeDisplay}", FontSize = 11, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 2, 0, 0) });
            vendorCard.Child = vendorStack;
            Grid.SetColumn(vendorCard, 0);
            metaGrid.Children.Add(vendorCard);

            // Receiver Card
            var receiverCard = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var receiverStack = new StackPanel();
            receiverStack.Children.Add(new TextBlock { Text = "RECEIVER & STORE INFO", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 0, 0, 4) });
            receiverStack.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(purchase.ReceiverName) ? "Central Warehouse Desk" : purchase.ReceiverName, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkBrush") });
            receiverStack.Children.Add(new TextBlock { Text = $"Phone: {(string.IsNullOrWhiteSpace(purchase.ReceiverNumber) ? "—" : purchase.ReceiverNumber)}", FontSize = 11, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 2, 0, 0) });
            receiverCard.Child = receiverStack;
            Grid.SetColumn(receiverCard, 2);
            metaGrid.Children.Add(receiverCard);

            ContentStack.Children.Add(metaGrid);

            // 3. Purchase Items Table Header Bar
            var tableHeaderBar = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#1E293B")!,
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 0)
            };
            var itemsHeader = new Grid();
            itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
            itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.3, GridUnitType.Star) });
            itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
            itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
            itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            itemsHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            AddWhiteHeaderCell(itemsHeader, "#", 0, TextAlignment.Center);
            AddWhiteHeaderCell(itemsHeader, "DESCRIPTION & CATEGORY", 1, TextAlignment.Left);
            AddWhiteHeaderCell(itemsHeader, "QTY", 2, TextAlignment.Right);
            AddWhiteHeaderCell(itemsHeader, "PRICE (₹)", 3, TextAlignment.Right);
            AddWhiteHeaderCell(itemsHeader, "GST %", 4, TextAlignment.Right);
            AddWhiteHeaderCell(itemsHeader, "TAX (₹)", 5, TextAlignment.Right);
            AddWhiteHeaderCell(itemsHeader, "TOTAL (₹)", 6, TextAlignment.Right);

            tableHeaderBar.Child = itemsHeader;
            ContentStack.Children.Add(tableHeaderBar);

            decimal grandSubtotal = 0;
            decimal grandGst = 0;
            decimal grandTotal = 0;

            int rowIdx = 1;
            foreach (var item in purchaseItems)
            {
                var qty = decimal.TryParse(item.Qty, out var q) ? q : 0;
                var price = decimal.TryParse(item.Price, out var p) ? p : 0;
                var gst = item.GstPercent;
                var subtotal = qty * price;
                var gstAmt = subtotal * (gst / 100m);
                var totalVal = item.TotalValue > 0 ? item.TotalValue : (subtotal + gstAmt);

                grandSubtotal += subtotal;
                grandGst += gstAmt;
                grandTotal += totalVal;

                var rowBorder = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString(rowIdx % 2 == 1 ? "#FFFFFF" : "#F8FAFC")!,
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(10, 7, 10, 7)
                };

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.3, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

                AddBodyCell(row, rowIdx.ToString(), 0, TextAlignment.Center);

                var nameText = new TextBlock
                {
                    Text = string.IsNullOrEmpty(item.Category) ? item.ProductName : $"{item.ProductName}\n[{item.Category}]",
                    FontSize = 12,
                    Foreground = (Brush)FindResource("InkBrush"),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(nameText, 1);
                row.Children.Add(nameText);

                AddBodyCell(row, qty.ToString("N0"), 2, TextAlignment.Right);
                AddBodyCell(row, $"₹{price:N2}", 3, TextAlignment.Right);
                AddBodyCell(row, $"{gst:G29}%", 4, TextAlignment.Right);
                AddBodyCell(row, $"₹{gstAmt:N2}", 5, TextAlignment.Right);
                AddBodyCell(row, $"₹{totalVal:N2}", 6, TextAlignment.Right, isBold: true);

                rowBorder.Child = row;
                ContentStack.Children.Add(rowBorder);
                rowIdx++;
            }

            // 4. Notes & Summary Layout Grid
            var bottomGrid = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            // Left: Notes Card
            var notesCard = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var notesStack = new StackPanel();
            notesStack.Children.Add(new TextBlock { Text = "NOTES & ACKNOWLEDGEMENT", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("InkSoftBrush"), Margin = new Thickness(0, 0, 0, 4) });
            notesStack.Children.Add(new TextBlock { Text = "1. Goods received in good condition as per invoice.", FontSize = 10.5, Foreground = (Brush)FindResource("InkBrush"), Margin = new Thickness(0, 1, 0, 1) });
            notesStack.Children.Add(new TextBlock { Text = "2. Invoice entry recorded in Stock Portal system.", FontSize = 10.5, Foreground = (Brush)FindResource("InkBrush"), Margin = new Thickness(0, 1, 0, 1) });
            notesCard.Child = notesStack;
            Grid.SetColumn(notesCard, 0);
            bottomGrid.Children.Add(notesCard);

            // Right: Calculation Summary Box
            var summaryStack = new StackPanel();
            summaryStack.Children.Add(CreateSummaryRow("Subtotal (Excl. Tax):", $"₹{grandSubtotal:N2}"));
            summaryStack.Children.Add(CreateSummaryRow("Total GST Tax:", $"₹{grandGst:N2}"));

            var grandTotalBorder = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#FFF5EC")!,
                BorderBrush = (Brush)FindResource("SaffronDeepBrush"),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 6, 0, 0)
            };
            var gtGrid = new Grid();
            gtGrid.ColumnDefinitions.Add(new ColumnDefinition());
            gtGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            gtGrid.Children.Add(new TextBlock { Text = "GRAND TOTAL:", FontWeight = FontWeights.Bold, FontSize = 13, Foreground = (Brush)FindResource("InkBrush") });
            var gtVal = new TextBlock { Text = $"₹{grandTotal:N2}", FontWeight = FontWeights.ExtraBold, FontSize = 15, Foreground = (Brush)FindResource("SaffronDeepBrush") };
            Grid.SetColumn(gtVal, 1);
            gtGrid.Children.Add(gtVal);
            grandTotalBorder.Child = gtGrid;

            summaryStack.Children.Add(grandTotalBorder);
            Grid.SetColumn(summaryStack, 2);
            bottomGrid.Children.Add(summaryStack);

            ContentStack.Children.Add(bottomGrid);

            // 5. Distribution section (Direct purchases only)
            if (distribution.HasValue)
            {
                var d = distribution.Value;

                ContentStack.Children.Add(new Border { Height = 1, Background = (Brush)FindResource("LineBrush"), Margin = new Thickness(0, 18, 0, 16) });

                var distBadge = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#FFF0DE")!,
                    CornerRadius = new CornerRadius(999),
                    Padding = new Thickness(12, 5, 12, 5),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 12)
                };
                distBadge.Child = new TextBlock { Text = "Goods Dispatched", Foreground = (Brush)new BrushConverter().ConvertFromString("#E2600A")!, FontWeight = FontWeights.Bold, FontSize = 11.5 };
                ContentStack.Children.Add(distBadge);

                AddMetaRow("Shipment ID", d.ShipmentId);
                AddMetaRow("Destination", d.Destination);
                AddMetaRow("Dispatch Date", d.Date?.ToString("dd MMM yyyy"));
                AddMetaRow("Received By", d.ReceiverName);
                AddMetaRow("Receiver Phone", d.ReceiverNumber);
            }

            // 6. Signatures Block
            var sigGrid = new Grid { Margin = new Thickness(0, 32, 0, 0) };
            sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var prepStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            prepStack.Children.Add(new Border { Height = 1, Width = 160, Background = (Brush)FindResource("LineBrush"), Margin = new Thickness(0, 0, 0, 4) });
            prepStack.Children.Add(new TextBlock { Text = "Received & Checked By", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkSoftBrush") });
            Grid.SetColumn(prepStack, 0);
            sigGrid.Children.Add(prepStack);

            var authStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            authStack.Children.Add(new Border { Height = 1, Width = 160, Background = (Brush)FindResource("LineBrush"), Margin = new Thickness(0, 0, 0, 4) });
            authStack.Children.Add(new TextBlock { Text = "Authorized Manager & Stamp", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkSoftBrush"), HorizontalAlignment = HorizontalAlignment.Right });
            Grid.SetColumn(authStack, 1);
            sigGrid.Children.Add(authStack);

            ContentStack.Children.Add(sigGrid);

            // 7. Footer Note
            ContentStack.Children.Add(new TextBlock
            {
                Text = "This is an official system-generated Purchase Receipt from Stock Portal.",
                FontSize = 10,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                Margin = new Thickness(0, 24, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        private void AddWhiteHeaderCell(Grid grid, string text, int col, TextAlignment alignment)
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
            var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = label, FontSize = 11.5, Foreground = (Brush)FindResource("InkSoftBrush") });
            var val = new TextBlock { Text = string.IsNullOrWhiteSpace(value) ? "—" : value, FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("InkBrush") };
            Grid.SetColumn(val, 1);
            row.Children.Add(val);
            ContentStack.Children.Add(row);
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e) => Close();

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                double pageWidth = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 794;
                double pageHeight = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 1123;

                // 1. Temporarily detach PrintContentPanel from window ScrollViewer
                ReceiptScrollViewer.Content = null;

                // 2. Create padded A4 print wrapper container
                var printWrapper = new Border
                {
                    Width = pageWidth,
                    Height = pageHeight,
                    Background = Brushes.White,
                    Padding = new Thickness(35, 35, 35, 35) // Safe 35px print margins on all 4 edges
                };

                PrintContentPanel.Width = double.NaN; // Fill wrapper width automatically
                PrintContentPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                PrintContentPanel.BorderThickness = new Thickness(0); // Remove outer card border for paper
                printWrapper.Child = PrintContentPanel;

                // 3. Measure & Arrange printWrapper to exact printer page dimensions
                var pSize = new Size(pageWidth, pageHeight);
                printWrapper.Measure(pSize);
                printWrapper.Arrange(new Rect(new Point(0, 0), pSize));
                printWrapper.UpdateLayout();

                try
                {
                    printDialog.PrintVisual(printWrapper, "Purchase Receipt Document");
                }
                finally
                {
                    // 4. Restore PrintContentPanel back to Window preview
                    printWrapper.Child = null;
                    PrintContentPanel.Width = 620;
                    PrintContentPanel.BorderThickness = new Thickness(1);
                    PrintContentPanel.HorizontalAlignment = HorizontalAlignment.Center;
                    ReceiptScrollViewer.Content = PrintContentPanel;
                }
            }
        }
    }
}
