using System;

namespace StockPortalApp.Models
{
    public class FixedAssetHistoryRecord
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ActionType { get; set; } = "Mapped"; // "Mapped", "Moved", "Status Change"
        public string? FromLocation { get; set; }
        public string ToLocation { get; set; } = "";
        public string? Remarks { get; set; }
        public DateTime ActionDate { get; set; }

        public string TitleDisplay
        {
            get
            {
                if (string.Equals(ActionType, "Mapped", StringComparison.OrdinalIgnoreCase))
                    return $"Mapped — {ToLocation}";
                if (string.Equals(ActionType, "Moved", StringComparison.OrdinalIgnoreCase))
                    return $"Moved — to {ToLocation}";
                if (string.Equals(ActionType, "Status Change", StringComparison.OrdinalIgnoreCase))
                    return $"Status — {ToLocation}";
                return $"{ActionType} — {ToLocation}";
            }
        }

        public string RemarksDisplay => string.IsNullOrWhiteSpace(Remarks) ? "" : $"({Remarks})";
        public bool HasRemarks => !string.IsNullOrWhiteSpace(Remarks);
        public string DateDisplay => ActionDate > DateTime.MinValue ? ActionDate.ToString("dd MMM yyyy") : "";
    }
}
