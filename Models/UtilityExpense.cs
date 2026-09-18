using System;

namespace StockPortalApp.Models
{
    public class UtilityExpense
    {
        public int Id { get; set; }
        public string Category { get; set; } = "Plumbing";
        public string Location { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; } = DateTime.Today;
        public string? Description { get; set; }
        public string? BillNo { get; set; }
        public string? BillImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Display Helpers for DataGrid
        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "General" : Category;
        public string LocationDisplay => string.IsNullOrWhiteSpace(Location) ? "—" : Location;
        public string AmountDisplay => Amount > 0 ? $"₹{Amount:N2}" : "—";
        public string ExpenseDateDisplay => ExpenseDate.ToString("dd MMM yyyy");
        public string BillNoDisplay => string.IsNullOrWhiteSpace(BillNo) ? "—" : BillNo;
        public string DescriptionDisplay => string.IsNullOrWhiteSpace(Description) ? "—" : Description;
        public bool HasBillImage => !string.IsNullOrWhiteSpace(BillImagePath) && System.IO.File.Exists(BillImagePath);
        public string BillImageButtonText => HasBillImage ? "View Image" : "No Image";
        public string CreatedAtDisplay => CreatedAt == DateTime.MinValue ? "—" : CreatedAt.ToString("dd MMM yyyy hh:mm tt");
    }
}
