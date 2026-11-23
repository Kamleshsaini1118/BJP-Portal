using System;
using System.Collections.Generic;

namespace StockPortal.Core.DTOs.Distributions
{
    public class DistributionSummaryDto
    {
        public int Id { get; set; }
        public string ShipmentId { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DistributionDate { get; set; }
        public DateTime Date { get => DistributionDate; set => DistributionDate = value; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Position { get; set; }
        public string? Remark { get; set; }
        public string ItemsList { get; set; } = string.Empty;
        public decimal TotalQty { get; set; }
        public List<DistributionItemRowDto> Items { get; set; } = new();
    }

    public class DistributionItemRowDto
    {
        public int Id { get; set; }
        public int DistributionId { get; set; }
        public string? Category { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
    }

    public class SaveDistributionDto
    {
        public int? Id { get; set; }
        public string? ShipmentId { get; set; }
        public string Destination { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Position { get; set; }
        public string? Remark { get; set; }
        public List<DistributionItemRowDto> Items { get; set; } = new();
    }

    public class SaveDistributionResultDto
    {
        public int Id { get; set; }
        public string ShipmentId { get; set; } = string.Empty;
    }

    public class DistributionOfficeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactNumber { get; set; }
    }
}
