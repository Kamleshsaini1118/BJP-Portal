using System;

namespace StockPortal.Core.DTOs.FoodPackets
{
    public class FoodPacketBatchDto
    {
        public int Id { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? MealFor { get; set; }
        public string? VendorName { get; set; }
        public string? MeetingCampaign { get; set; }
        public decimal Qty { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal DispatchedQty { get; set; }
        public DateTime BatchDate { get; set; }
        public int DispatchCount { get; set; }
    }

    public class CreateFoodPacketBatchDto
    {
        public string ItemName { get; set; } = string.Empty;
        public string? MealFor { get; set; }
        public string? VendorName { get; set; }
        public string? MeetingCampaign { get; set; }
        public decimal Qty { get; set; }
        public DateTime BatchDate { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
    }

    public class DispatchFoodPacketDto
    {
        public int BatchId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public string PersonNumber { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public DateTime DispatchDate { get; set; }
    }

    public class BatchDispatchRecordDto
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public string PersonNumber { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FoodPacketLedgerDto
    {
        public string Type { get; set; } = string.Empty;
        public string BatchCode { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Party { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
