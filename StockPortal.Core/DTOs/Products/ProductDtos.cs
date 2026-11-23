using System;

namespace StockPortal.Core.DTOs.Products
{
    public class ProductCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public bool? IsActive { get; set; } = true;
        public int? ProductCount { get; set; } = 0;
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? CustomPrefix { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductDto
    {
        public int? Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal? MinQty { get; set; }
        public decimal? AvgPrice { get; set; }
        public string ProductType { get; set; } = "Moving Asset";
        public object? CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveProductDto
    {
        public int? Id { get; set; }
        public string? ProductCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal? MinQty { get; set; }
        public decimal? AvgPrice { get; set; }
        public string ProductType { get; set; } = "Moving Asset";
        public bool IsActive { get; set; } = true;
    }

    public class ToggleStatusDto
    {
        public bool IsActive { get; set; }
    }
}
