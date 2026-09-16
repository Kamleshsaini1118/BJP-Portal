using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Models
{
    public class DamageRecord
    {
        public int Id { get; set; }
        public string DamageCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string? Category { get; set; }
        public decimal Qty { get; set; }
        public string? ReporteeName { get; set; }
        public string? ReporteeNumber { get; set; }
        public string? ReporteePosition { get; set; }
        public System.DateTime DamageDate { get; set; }
        public string? Remark { get; set; }
        public string? ImagePath { get; set; }

        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "—" : Category;
        public string QtyDisplay => Qty.ToString("N0");
        public string RemarkDisplay => string.IsNullOrWhiteSpace(Remark) ? "—" : Remark;
        public string DateDisplay => DamageDate.ToString("dd MMM yyyy");

        public List<string> GetImagePaths()
        {
            if (string.IsNullOrWhiteSpace(ImagePath)) return new List<string>();
            return ImagePath.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => !string.IsNullOrEmpty(p))
                            .ToList();
        }
    }
}
