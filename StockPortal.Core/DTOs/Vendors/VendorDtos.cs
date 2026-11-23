using System;

namespace StockPortal.Core.DTOs.Vendors
{
    public class VendorDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Gst { get; set; }
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? BankHolder { get; set; }
        public string? BankAccount { get; set; }
        public string? BankIfsc { get; set; }
        public string? BankBranch { get; set; }
        public object? CreatedAt { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    public class SaveVendorDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Gst { get; set; }
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? BankHolder { get; set; }
        public string? BankAccount { get; set; }
        public string? BankIfsc { get; set; }
        public string? BankBranch { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
