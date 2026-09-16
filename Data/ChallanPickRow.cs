using System.ComponentModel;
using StockPortalApp.Models;

namespace StockPortalApp
{
    /// <summary>A checkable row in the Combine Challans picker, wrapping one pending challan purchase.</summary>
    public class ChallanPickRow : INotifyPropertyChanged
    {
        public PendingChallan Challan { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnChanged(nameof(IsSelected)); }
        }

        public ChallanPickRow(PendingChallan challan)
        {
            Challan = challan;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}