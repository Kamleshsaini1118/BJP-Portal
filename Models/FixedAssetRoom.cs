using System;
using System.Collections.Generic;

namespace StockPortalApp.Models
{
    public class FixedAssetRoom
    {
        public int RoomId { get; set; }
        public int KaryalyaId { get; set; }
        public string Karyalya { get; set; } = "";
        public int DepartmentId { get; set; }
        public string Department { get; set; } = "";
        public string RoomNo { get; set; } = "";
        public string? Remarks { get; set; }
        public int TotalAssets { get; set; }
        public int ActiveCount { get; set; }
        public int DamagedCount { get; set; }
        public string ItemsSummary { get; set; } = "";

        // Display Helpers
        public string RoomNoDisplay => string.IsNullOrWhiteSpace(RoomNo) ? "—" : RoomNo;
        public string TotalAssetsDisplay => TotalAssets.ToString();
        public string ActiveCountDisplay => ActiveCount.ToString();
        public string DamagedCountDisplay => DamagedCount.ToString();
    }
}
