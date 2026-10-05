using System;

namespace StockPortalApp.Models
{
    public class Karyalya
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string InchargeName { get; set; } = "";
        public string InchargeContact { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public string CreatedAt { get; set; } = "";

        public override string ToString() => Name;
    }
}
