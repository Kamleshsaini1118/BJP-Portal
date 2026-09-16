using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using StockPortalApp.Data;
 using StockPortalApp.Helpers;
using StockPortalApp.Models;

namespace StockPortalApp
{
    /// <summary>Editable row shown in the Mark Deposited popup — wraps one issued item with live-bindable fields.</summary>
    public class DepositEntryRow : INotifyPropertyChanged
    {
        public int Id { get; }
        public string ProductName { get; }
        public decimal IssuedQty { get; }

        public string IssuedLabel => $"  (Issued: {IssuedQty:N0})";

        private string _depositedCount;
        public string DepositedCount
        {
            get => _depositedCount;
            set
            {
                if (decimal.TryParse(value, out var parsed))
                {
                    if (parsed > IssuedQty)
                    {
                        parsed = IssuedQty;
                        value = parsed.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                _depositedCount = value;
                OnChanged(nameof(DepositedCount));
                OnChanged(nameof(IsShortfall));
                OnChanged(nameof(ShortfallReasonVisibility));

                if (!IsShortfall)
                {
                    ShortfallReason = "";
                }
            }
        }

        private string _shortfallReason = "";
        public string ShortfallReason
        {
            get => _shortfallReason;
            set { _shortfallReason = value; OnChanged(nameof(ShortfallReason)); }
        }

        public bool IsShortfall => string.IsNullOrWhiteSpace(DepositedCount) || (decimal.TryParse(DepositedCount, out var d) && d < IssuedQty);

        public Visibility ShortfallReasonVisibility => IsShortfall ? Visibility.Visible : Visibility.Collapsed;

        public DepositEntryRow(DepositItemEntry source)
        {
            Id = source.Id;
            ProductName = source.ProductName;
            IssuedQty = source.IssuedQty;
            _depositedCount = source.IssuedQty.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MarkDepositedWindow : Window
    {
        private readonly int _issueRecordId;
        private readonly ObservableCollection<DepositEntryRow> _rows = new();

        public MarkDepositedWindow(IssueRecordHeader header, List<DepositItemEntry> items)
        {
            InitializeComponent();

            _issueRecordId = header.Id;
            HeaderTitle.Text = $"Mark Items Deposited — {header.EventName}";

            ReceiverNameBox.Text = header.ReceiverName;
            ReceiverNumberBox.Text = header.ReceiverNumber;
            ReceiverPositionBox.Text = header.ReceiverPosition;

            foreach (var item in items)
            {
                _rows.Add(new DepositEntryRow(item));
            }
            ItemRowsControl.ItemsSource = _rows;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var parsed = new List<(int Id, decimal Deposited, string? Reason)>();

            foreach (var row in _rows)
            {
                if (!decimal.TryParse(row.DepositedCount, out var deposited) || deposited < 0)
                {
                    MessageBox.Show($"Enter a valid deposited count for \"{row.ProductName}\".", "Check the numbers",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (deposited > row.IssuedQty)
                {
                    MessageBox.Show($"Deposited count for \"{row.ProductName}\" can't be more than the issued quantity ({row.IssuedQty:N0}).",
                        "Check the numbers", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var reason = row.ShortfallReason.Trim();
                if (deposited < row.IssuedQty && string.IsNullOrWhiteSpace(reason))
                {
                    MessageBox.Show($"\"{row.ProductName}\" is short by {row.IssuedQty - deposited:N0} — add a reason for the shortfall.",
                        "Missing information", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                parsed.Add((row.Id, deposited, string.IsNullOrWhiteSpace(reason) ? null : reason));
            }

            SubmitButton.IsEnabled = false;
            try
            {
                foreach (var (id, deposited, reason) in parsed)
                {
                    await IssueRepository.SetItemDepositAsync(id, deposited, reason);
                }

                await IssueRepository.MarkDepositedAsync(_issueRecordId, IndiaTime.Today);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save the deposit.\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}