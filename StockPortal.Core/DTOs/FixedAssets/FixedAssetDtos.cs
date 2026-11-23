using System;
using System.Collections.Generic;

namespace StockPortal.Core.DTOs.FixedAssets
{
    public class FixedAssetSummaryDto
    {
        public string? Category { get; set; }
        public string? Item { get; set; }
        public string? Code { get; set; }
        public decimal Qty { get; set; }
        public decimal Value { get; set; }

        public string ItemName { get => Item ?? string.Empty; set => Item = value; }
        public int TotalPurchased { get => (int)Qty; set => Qty = value; }
        public int MappedToRooms { get; set; }
        public int InStore => Math.Max(0, TotalPurchased - MappedToRooms);
    }

    public class AssetDistributionDetailDto
    {
        public string Destination { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public DateTime? Date { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverNumber { get; set; }
        public string? Remark { get; set; }
    }

    public class KaryalyaDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public int KaryalyaId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HeadName { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class FixedAssetRoomDto
    {
        public int Id { get; set; }
        public int RoomId { get => Id; set => Id = value; }

        public int KaryalyaId { get; set; }
        public string KaryalyaName { get; set; } = string.Empty;
        public string Karyalya { get => KaryalyaName; set => KaryalyaName = value; }

        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Department { get => DepartmentName; set => DepartmentName = value; }

        public string RoomNumber { get; set; } = string.Empty;
        public string RoomNo { get => RoomNumber; set => RoomNumber = value; }

        public string RoomName { get; set; } = string.Empty;
        public string? Remarks { get => RoomName; set => RoomName = value ?? string.Empty; }

        public string? InchargeName { get; set; }
        public string? InchargeNumber { get; set; }

        public int ItemCount { get; set; }
        public int TotalAssets { get => ItemCount; set => ItemCount = value; }
        public int ActiveCount { get; set; }
        public int DamagedCount { get; set; }
        public string ItemsSummary { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class FixedAssetRoomItemDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int ProductId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ProductName { get => ItemName; set => ItemName = value; }
        public string? SerialNumber { get; set; }
        public string? ItemUniqueCode { get => SerialNumber; set => SerialNumber = value; }
        public string? Barcode { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public DateTime MappingDate { get => AssignedDate; set => AssignedDate = value; }
    }

    public class SaveRoomMappingDto
    {
        public FixedAssetRoomDto Room { get; set; } = new();
        public List<FixedAssetRoomItemDto> Items { get; set; } = new();
    }

    public class FixedAssetReportItemDto
    {
        public int Id { get; set; }
        public int ItemId { get => Id; set => Id = value; }
        public int RoomId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;

        public int KaryalyaId { get; set; }
        public string KaryalyaName { get; set; } = string.Empty;

        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;

        public string RoomNumber { get; set; } = string.Empty;
        public string RoomNo { get => RoomNumber; set => RoomNumber = value; }
        public string RoomName { get; set; } = string.Empty;
        public string RoomRemarks { get => RoomName; set => RoomName = value; }

        public string ItemName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string ItemUniqueCode { get => SerialNumber ?? string.Empty; set => SerialNumber = value; }
        public string? Barcode { get; set; }

        public string Status { get; set; } = "Active";
        public DateTime AssignedDate { get; set; }
        public DateTime MappedOn { get => AssignedDate; set => AssignedDate = value; }
        public DateTime LastUpdated { get; set; }

        public string? InchargeName { get; set; }
        public string? InchargeNumber { get; set; }
        public string InchargeContact { get => InchargeNumber ?? string.Empty; set => InchargeNumber = value; }
    }

    public class FixedAssetHistoryRecordDto
    {
        public int Id { get; set; }
        public int RoomItemId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime ActionDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class MoveAssetRequestDto
    {
        public int ItemId { get; set; }
        public int ToRoomId { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class UpdateAssetStatusDto
    {
        public int ItemId { get; set; }
        public string NewStatus { get; set; } = "Active";
        public string? Remarks { get; set; }
    }
}