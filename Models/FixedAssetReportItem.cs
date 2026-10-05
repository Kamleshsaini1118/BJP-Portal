using System;

namespace StockPortalApp.Models
{
    public class FixedAssetReportItem
    {
        public int ItemId { get; set; }
        public int RoomId { get; set; }
        public int ProductId { get; set; }
        public string ItemUniqueCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public int KaryalyaId { get; set; }
        public string KaryalyaName { get; set; } = "";
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "";
        public string InchargeName { get; set; } = "";
        public string InchargeContact { get; set; } = "";
        public string RoomRemarks { get; set; } = "";

        public string InchargeDisplay
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(InchargeName) && !string.IsNullOrWhiteSpace(InchargeContact))
                    return $"{InchargeName} • {InchargeContact}";
                if (!string.IsNullOrWhiteSpace(InchargeName))
                    return InchargeName;
                return InchargeContact ?? "";
            }
        }

        public string RoomNo { get; set; } = "";
        public string RoomNoDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(RoomNo)) return "-";
                if (RoomNo.StartsWith("Room", StringComparison.OrdinalIgnoreCase)) return RoomNo;
                return $"Room No. {RoomNo}";
            }
        }

        public DateTime MappedOn { get; set; }
        public string MappedOnDisplay => MappedOn > DateTime.MinValue ? MappedOn.ToString("dd MMM yyyy") : "-";

        public string Status { get; set; } = "Active";
        public bool IsActiveStatus => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);

        public DateTime LastUpdated { get; set; }
        public string LastUpdatedDisplay => LastUpdated > DateTime.MinValue ? LastUpdated.ToString("dd MMM yyyy") : MappedOnDisplay;
    }

    public class FixedAssetKaryalyaGroup
    {
        public int KaryalyaId { get; set; }
        public string KaryalyaName { get; set; } = "";
        public int DepartmentsCount { get; set; }
        public int RoomsCount { get; set; }
        public int TotalAssets { get; set; }
        public int ActiveCount { get; set; }
        public int DamagedCount { get; set; }
        public string ItemsSummary { get; set; } = "";
    }

    public class FixedAssetDepartmentGroup
    {
        public int DepartmentId { get; set; }
        public int KaryalyaId { get; set; }
        public string KaryalyaName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string InchargeDisplay { get; set; } = "";
        public int RoomsCount { get; set; }
        public int TotalAssets { get; set; }
        public int ActiveCount { get; set; }
        public int DamagedCount { get; set; }
        public string ItemsSummary { get; set; } = "";
    }

    public class FixedAssetItemGroup
    {
        public int ProductId { get; set; }
        public string ItemName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public int KaryalyasCount { get; set; }
        public int RoomsCount { get; set; }
        public int TotalAssets { get; set; }
        public int ActiveCount { get; set; }
        public int DamagedCount { get; set; }
        public string UniqueCodesSummary { get; set; } = "";
    }
}
