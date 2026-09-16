namespace StockPortalApp.Models
{
    /// <summary>Full header fields for one issue record, used to prefill the Edit form.</summary>
    public class IssueRecordHeader
    {
        public int Id { get; set; }
        public string IssueCode { get; set; } = "";
        public string EventName { get; set; } = "";
        public System.DateTime? EventStartDate { get; set; }
        public System.DateTime? EventEndDate { get; set; }
        public System.DateTime DepositDate { get; set; }
        public string? VenueAddress { get; set; }
        public string ReceiverName { get; set; } = "";
        public string ReceiverNumber { get; set; } = "";
        public string? ReceiverPosition { get; set; }
        public string? Remark { get; set; }
        public string Status { get; set; } = "";
        public System.DateTime? DepositedOn { get; set; }
        public System.DateTime? CreatedAt { get; set; }

        public string IssueCodeDisplay => string.IsNullOrWhiteSpace(IssueCode) ? "—" : IssueCode;
        public string EventStartDateDisplay => EventStartDate.HasValue ? EventStartDate.Value.ToString("dd MMM yyyy") : "—";
        public string EventEndDateDisplay => EventEndDate.HasValue ? EventEndDate.Value.ToString("dd MMM yyyy") : "—";
        public string DepositDateDisplay => DepositDate.ToString("dd MMM yyyy");
        public string DepositedOnDisplay => DepositedOn.HasValue ? DepositedOn.Value.ToString("dd MMM yyyy") : "—";
        public string CreatedAtDisplay => CreatedAt.HasValue ? CreatedAt.Value.ToString("dd MMM yyyy") : "—";
    }
}