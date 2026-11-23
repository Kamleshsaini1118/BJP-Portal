using System;
using System.Collections.Generic;

namespace StockPortal.Core.DTOs.Quotations
{
    public class QuotationProjectDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Open";
        public int QuotationCount { get; set; }
        public decimal? LowestAmount { get; set; }
        public List<VendorQuotationDto> Quotations { get; set; } = new();
    }

    public class VendorQuotationDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal QuotationAmount { get; set; }
        public string? Remarks { get; set; }
        public string? FilePath { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected"
    }

    public class SaveQuotationProjectDto
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Open";
    }

    public class SaveVendorQuotationDto
    {
        public int? Id { get; set; }
        public int ProjectId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal QuotationAmount { get; set; }
        public string? Remarks { get; set; }
        public string? FilePath { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
