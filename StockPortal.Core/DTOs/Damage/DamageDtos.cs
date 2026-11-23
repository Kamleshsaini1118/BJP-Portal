using System;

namespace StockPortal.Core.DTOs.Damage
{
    public class DamageRecordDto
    {
        public int Id { get; set; }
        public string DamageCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal Qty { get; set; }
        public string? ReporteeName { get; set; }
        public string? ReporteeNumber { get; set; }
        public string? ReporteePosition { get; set; }
        public DateTime DamageDate { get; set; }
        public string? Remark { get; set; }
        public string? ImagePath { get; set; }
    }

    public class CreateDamageRecordDto
    {
        public string? DamageCode { get; set; }
        public string? Category { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string? ReporteeName { get; set; }
        public string? ReporteeNumber { get; set; }
        public string? ReporteePosition { get; set; }
        public string? Remark { get; set; }
        public string? ImageBase64 { get; set; }
        public string? ImageFileName { get; set; }
        public string? ImagePath { get; set; }
    }
}
