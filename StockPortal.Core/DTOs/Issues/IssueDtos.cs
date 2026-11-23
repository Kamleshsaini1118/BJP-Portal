using System;
using System.Collections.Generic;

namespace StockPortal.Core.DTOs.Issues
{
    public class IssueRecordLineDto
    {
        public int Id { get; set; }
        public string IssueCode { get; set; } = string.Empty;

        // Exact SQL Columns from sp_GetIssueRecords
        public string EventName { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public DateTime? DepositDate { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverNumber { get; set; } = string.Empty;
        public string? ReceiverPosition { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? CreatedAt { get; set; }
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }

        // Aliases for compatibility with existing code
        public string IssuedTo
        {
            get => !string.IsNullOrEmpty(ReceiverName) ? ReceiverName : _issuedTo;
            set { _issuedTo = value; if (string.IsNullOrEmpty(ReceiverName)) ReceiverName = value; }
        }
        private string _issuedTo = string.Empty;

        public string? PhoneNumber
        {
            get => !string.IsNullOrEmpty(ReceiverNumber) ? ReceiverNumber : _phoneNumber;
            set { _phoneNumber = value; if (string.IsNullOrEmpty(ReceiverNumber) && value != null) ReceiverNumber = value; }
        }
        private string? _phoneNumber;

        public string? Designation
        {
            get => !string.IsNullOrEmpty(ReceiverPosition) ? ReceiverPosition : _designation;
            set { _designation = value; if (string.IsNullOrEmpty(ReceiverPosition)) ReceiverPosition = value; }
        }
        private string? _designation;

        public string Purpose
        {
            get => !string.IsNullOrEmpty(EventName) ? EventName : _purpose;
            set { _purpose = value; if (string.IsNullOrEmpty(EventName)) EventName = value; }
        }
        private string _purpose = string.Empty;

        public DateTime IssueDate
        {
            get => EventStartDate ?? _issueDate;
            set { _issueDate = value; if (!EventStartDate.HasValue) EventStartDate = value; }
        }
        private DateTime _issueDate;

        public DateTime? ExpectedReturnDate
        {
            get => DepositDate ?? EventEndDate ?? _expectedReturnDate;
            set { _expectedReturnDate = value; if (!DepositDate.HasValue) DepositDate = value; }
        }
        private DateTime? _expectedReturnDate;

        public string? Remarks { get; set; }
        public List<IssueItemRowDto> Items { get; set; } = new();
    }

    public class IssueRecordHeaderDto
    {
        public int Id { get; set; }
        public string IssueCode { get; set; } = string.Empty;

        // Exact SQL Columns from sp_GetIssueRecordHeader
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }
        public DateTime DepositDate { get; set; }
        public string? VenueAddress { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverNumber { get; set; } = string.Empty;
        public string? ReceiverPosition { get; set; }
        public string? Remark { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? DepositedOn { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Aliases for compatibility with existing code
        public string IssuedTo
        {
            get => !string.IsNullOrEmpty(ReceiverName) ? ReceiverName : _issuedTo;
            set { _issuedTo = value; if (string.IsNullOrEmpty(ReceiverName)) ReceiverName = value; }
        }
        private string _issuedTo = string.Empty;

        public string? PhoneNumber
        {
            get => !string.IsNullOrEmpty(ReceiverNumber) ? ReceiverNumber : _phoneNumber;
            set { _phoneNumber = value; if (string.IsNullOrEmpty(ReceiverNumber) && value != null) ReceiverNumber = value; }
        }
        private string? _phoneNumber;

        public string? Designation
        {
            get => !string.IsNullOrEmpty(ReceiverPosition) ? ReceiverPosition : _designation;
            set { _designation = value; if (string.IsNullOrEmpty(ReceiverPosition)) ReceiverPosition = value; }
        }
        private string? _designation;

        public string Purpose
        {
            get => !string.IsNullOrEmpty(EventName) ? EventName : _purpose;
            set { _purpose = value; if (string.IsNullOrEmpty(EventName)) EventName = value; }
        }
        private string _purpose = string.Empty;

        public DateTime IssueDate
        {
            get => EventStartDate ?? _issueDate;
            set { _issueDate = value; if (!EventStartDate.HasValue) EventStartDate = value; }
        }
        private DateTime _issueDate;

        public DateTime? ExpectedReturnDate
        {
            get => DepositDate != default ? DepositDate : (EventEndDate ?? _expectedReturnDate);
            set { _expectedReturnDate = value; }
        }
        private DateTime? _expectedReturnDate;

        public string? Remarks
        {
            get => Remark ?? _remarks;
            set { _remarks = value; if (Remark == null) Remark = value; }
        }
        private string? _remarks;
    }

    public class IssueItemRowDto
    {
        public int Id { get; set; }
        public int IssueRecordId { get; set; }
        public string? Category { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal DepositedQty { get; set; }
        public string? ShortfallReason { get; set; }
    }

    public class SaveIssueDto
    {
        public int? Id { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime? EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }
        public DateTime? DepositDate { get; set; }
        public string? VenueAddress { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverNumber { get; set; } = string.Empty;
        public string? ReceiverPosition { get; set; }
        public string? Remark { get; set; }

        // Aliases
        public string IssuedTo
        {
            get => !string.IsNullOrEmpty(ReceiverName) ? ReceiverName : _issuedTo;
            set { _issuedTo = value; if (string.IsNullOrEmpty(ReceiverName)) ReceiverName = value; }
        }
        private string _issuedTo = string.Empty;

        public string? PhoneNumber
        {
            get => !string.IsNullOrEmpty(ReceiverNumber) ? ReceiverNumber : _phoneNumber;
            set { _phoneNumber = value; if (string.IsNullOrEmpty(ReceiverNumber) && value != null) ReceiverNumber = value; }
        }
        private string? _phoneNumber;

        public string? Designation
        {
            get => !string.IsNullOrEmpty(ReceiverPosition) ? ReceiverPosition : _designation;
            set { _designation = value; if (string.IsNullOrEmpty(ReceiverPosition)) ReceiverPosition = value; }
        }
        private string? _designation;

        public string Purpose
        {
            get => !string.IsNullOrEmpty(EventName) ? EventName : _purpose;
            set { _purpose = value; if (string.IsNullOrEmpty(EventName)) EventName = value; }
        }
        private string _purpose = string.Empty;

        public DateTime IssueDate
        {
            get => EventStartDate ?? _issueDate;
            set { _issueDate = value; if (!EventStartDate.HasValue) EventStartDate = value; }
        }
        private DateTime _issueDate;

        public DateTime? ExpectedReturnDate
        {
            get => DepositDate ?? EventEndDate ?? _expectedReturnDate;
            set { _expectedReturnDate = value; if (!DepositDate.HasValue) DepositDate = value; }
        }
        private DateTime? _expectedReturnDate;

        public string? Remarks
        {
            get => Remark ?? _remarks;
            set { _remarks = value; if (Remark == null) Remark = value; }
        }
        private string? _remarks;

        public List<IssueItemRowDto> Items { get; set; } = new();
    }

    public class DepositItemEntryDto
    {
        public int ItemId { get; set; }
        public decimal DepositedQty { get; set; }
        public string? ShortfallReason { get; set; }
    }

    public class MarkDepositedRequestDto
    {
        public int IssueRecordId { get; set; }
        public DateTime DepositedOn { get; set; }
        public List<DepositItemEntryDto> Items { get; set; } = new();
    }
}
