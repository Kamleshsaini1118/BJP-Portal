using System;

namespace StockPortalApp.Models
{
    public class FixedAssetRoomItem
    {
        public int? Id { get; set; }
        public int? RoomId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ItemUniqueCode { get; set; } = "";
        public DateTime MappingDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Active";

        public string MappingDateDisplay => MappingDate.ToString("dd/MM/yyyy");
    }
}
