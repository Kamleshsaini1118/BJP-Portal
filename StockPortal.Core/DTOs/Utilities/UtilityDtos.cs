using System;

namespace StockPortal.Core.DTOs.Utilities
{
    public class UtilityExpenseDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
        public string? BillNo { get; set; }
        public string? BillImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveUtilityExpenseDto
    {
        public int? Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Description { get; set; }
        public string? BillNo { get; set; }
        public string? BillImagePath { get; set; }
    }

    public class UtilityReminderDto
    {
        public int Id { get; set; }
        public string UtilityType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AccountIdentifier { get; set; } = string.Empty;
        public int DueDayOfMonth { get; set; }
        public int ReminderDaysBefore { get; set; }
        public decimal? EstimatedAmount { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveUtilityReminderDto
    {
        public int? Id { get; set; }
        public string UtilityType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string AccountIdentifier { get; set; } = string.Empty;
        public int DueDayOfMonth { get; set; }
        public int ReminderDaysBefore { get; set; }
        public decimal? EstimatedAmount { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
