using System;

namespace StockPortalApp.Models
{
    public class Department
    {
        public int Id { get; set; }
        public int KaryalyaId { get; set; }
        public string KaryalyaName { get; set; } = "";
        public string Name { get; set; } = "";
        public string InchargeName { get; set; } = "";
        public string InchargeContact { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public string CreatedAt { get; set; } = "";

        public override string ToString() => Name;
    }
}
