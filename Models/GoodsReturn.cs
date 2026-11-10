using System;

namespace StockPortalApp.Models
{
    public class GoodsReturn
    {
        public int Id { get; set; }
        public string ReturnNo { get; set; } = "";
        public DateTime ReturnDate { get; set; }
        public string VendorName { get; set; } = "";
        public int PurchaseId { get; set; }
        public string AgainstPurchaseRef { get; set; } = "";
        public DateTime? PurchaseDate { get; set; }
        public string? PoReference { get; set; }
        public string Reason { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public decimal TotalQty { get; set; }
        public string ItemsSummary { get; set; } = "";
        public string CategoriesList { get; set; } = "";
        public string ProductsList { get; set; } = "";
        public string? HandedOverTo { get; set; }
        public string? ContactNumber { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }

        // Optional vendor profile details (populated in sp_GetGoodsReturnDetails for Return Note)
        public string? VendorGst { get; set; }
        public string? VendorAddress { get; set; }
        public string? VendorContactPerson { get; set; }
        public string? VendorContactNumber { get; set; }

        public string ReturnDateDisplay => ReturnDate.ToString("dd MMM yyyy");
        public string PurchaseDateDisplay => PurchaseDate.HasValue ? PurchaseDate.Value.ToString("dd MMM yyyy") : "—";
        public bool HasPurchaseDate => PurchaseDate.HasValue;
        public string TotalAmountDisplay => TotalAmount % 1 == 0 ? $"₹{TotalAmount:N0}" : $"₹{TotalAmount:N2}";
        public string TotalQtyDisplay => TotalQty % 1 == 0 ? TotalQty.ToString("N0") : TotalQty.ToString("0.##");
        public string ItemsDisplay => string.IsNullOrWhiteSpace(ItemsSummary) ? "—" : ItemsSummary;
        public string HandedOverToDisplay => string.IsNullOrWhiteSpace(HandedOverTo) ? "—" : HandedOverTo;
        public string ContactNumberDisplay => string.IsNullOrWhiteSpace(ContactNumber) ? "—" : ContactNumber;
        public string RemarksDisplay => string.IsNullOrWhiteSpace(Remarks) ? "—" : Remarks;
    }
}
