using System;
using System.ComponentModel;

namespace StockPortalApp.Models
{
    public class ReturnablePurchaseItemRow : INotifyPropertyChanged
    {
        private string _returnQtyText = "";

        public int PurchaseItemId { get; set; }
        public string? Category { get; set; }
        public string ProductName { get; set; } = "";
        public string? SerialNumber { get; set; }
        public decimal PurchasedQty { get; set; }
        public decimal ReturnedEarlierQty { get; set; }
        public decimal CanReturnQty => Math.Max(0m, PurchasedQty - ReturnedEarlierQty);
        public decimal Rate { get; set; }
        public decimal GstPercent { get; set; }

        public bool CanEdit => CanReturnQty > 0m;

        public string ItemMetaDisplay
        {
            get
            {
                bool hasCat = !string.IsNullOrWhiteSpace(Category);
                bool hasSerial = !string.IsNullOrWhiteSpace(SerialNumber);
                if (hasCat && hasSerial) return $"{Category} · S/N: {SerialNumber}";
                if (hasCat) return Category!;
                if (hasSerial) return $"S/N: {SerialNumber}";
                return "";
            }
        }

        public bool HasItemMeta => !string.IsNullOrWhiteSpace(ItemMetaDisplay);

        public string PurchasedDisplay => FormatQty(PurchasedQty);
        public string ReturnedEarlierDisplay => FormatQty(ReturnedEarlierQty);
        public string CanReturnDisplay => FormatQty(CanReturnQty);
        public string RateDisplay => $"₹{Rate:N2}";
        public string GstDisplay => $"{GstPercent:0.##}%";

        public string ReturnQtyText
        {
            get => _returnQtyText;
            set
            {
                if (_returnQtyText == value) return;
                _returnQtyText = value ?? "";
                OnChanged(nameof(ReturnQtyText));
                OnChanged(nameof(ReturnQty));
                OnChanged(nameof(LineAmount));
                OnChanged(nameof(LineAmountDisplay));
                OnChanged(nameof(IsOverLimit));
                OnQuantityChanged?.Invoke();
            }
        }

        public decimal ReturnQty => decimal.TryParse(_returnQtyText.Trim(), out var q) && q > 0 ? q : 0m;

        public bool IsOverLimit => ReturnQty > CanReturnQty;

        public decimal LineAmount => Math.Round(ReturnQty * Rate * (1m + GstPercent / 100m), 2, MidpointRounding.AwayFromZero);

        public string LineAmountDisplay => LineAmount == 0 ? "₹0" : $"₹{LineAmount:N2}";

        public Action? OnQuantityChanged { get; set; }

        private static string FormatQty(decimal qty) =>
            qty % 1 == 0 ? qty.ToString("0") : qty.ToString("0.##");

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
