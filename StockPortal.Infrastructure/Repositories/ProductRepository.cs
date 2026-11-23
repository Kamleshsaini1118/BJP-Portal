using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Products;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDapperContext _context;

        public ProductRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<ProductCategoryDto>> GetCategoriesAsync(string? searchQuery = null, string statusFilter = "ALL")
        {
            using var conn = _context.CreateConnection();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SearchQuery", string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery);
                parameters.Add("@StatusFilter", string.IsNullOrWhiteSpace(statusFilter) || statusFilter == "ALL" ? null : statusFilter);

                var items = await conn.QueryAsync<ProductCategoryDto>(
                    "dbo.sp_GetProductCategories",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch
            {
                var sql = @"
                    SELECT pc.Id, pc.Name, pc.Prefix, ISNULL(pc.IsActive, 1) AS IsActive,
                           (SELECT COUNT(*) FROM dbo.Products p WHERE p.CategoryId = pc.Id) AS ProductCount
                    FROM dbo.ProductCategories pc
                    WHERE (@SearchQuery IS NULL OR pc.Name LIKE '%' + @SearchQuery + '%' OR pc.Prefix LIKE '%' + @SearchQuery + '%')
                      AND (@StatusFilter IS NULL OR @StatusFilter = 'ALL' 
                           OR (@StatusFilter = 'ACTIVE' AND ISNULL(pc.IsActive, 1) = 1)
                           OR (@StatusFilter = 'INACTIVE' AND ISNULL(pc.IsActive, 1) = 0))
                    ORDER BY pc.Name ASC";

                var fallback = await conn.QueryAsync<ProductCategoryDto>(sql, new
                {
                    SearchQuery = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery,
                    StatusFilter = string.IsNullOrWhiteSpace(statusFilter) ? "ALL" : statusFilter
                });
                return fallback.AsList();
            }
        }

        public async Task<ProductCategoryDto> AddCategoryAsync(string name, string? customPrefix = null, bool isActive = true)
        {
            using var conn = _context.CreateConnection();
            string prefix;

            if (!string.IsNullOrWhiteSpace(customPrefix))
            {
                prefix = customPrefix.Trim().ToUpperInvariant();
            }
            else
            {
                var existing = await conn.QueryAsync<string>("SELECT Prefix FROM dbo.ProductCategories");
                var used = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                string baseLetters = new string(name.Where(char.IsLetter).ToArray()).ToUpperInvariant();
                if (baseLetters.Length > 3) baseLetters = baseLetters.Substring(0, 3);
                if (baseLetters.Length == 0) baseLetters = "CAT";

                prefix = baseLetters;
                int n = 1;
                while (used.Contains(prefix))
                {
                    n++;
                    prefix = baseLetters.Substring(0, Math.Min(2, baseLetters.Length)) + n;
                }
            }

            var parameters = new DynamicParameters();
            parameters.Add("@Name", name);
            parameters.Add("@Prefix", prefix);
            parameters.Add("@IsActive", isActive);

            return await conn.QueryFirstOrDefaultAsync<ProductCategoryDto>(
                "dbo.sp_AddProductCategory",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> ToggleCategoryStatusAsync(int id, bool isActive)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@IsActive", isActive);

            int affected = await conn.ExecuteAsync(
                "dbo.sp_ToggleCategoryStatus",
                parameters,
                commandType: CommandType.StoredProcedure);

            return affected > 0;
        }

        public async Task<List<ProductDto>> GetProductsAsync()
        {
            using var conn = _context.CreateConnection();
            List<ProductDto> items;
            try
            {
                var result = await conn.QueryAsync<ProductDto>(
                    "dbo.sp_GetProducts",
                    commandType: CommandType.StoredProcedure);
                items = result.AsList();
            }
            catch
            {
                var fallback = await conn.QueryAsync<ProductDto>(
                    @"SELECT p.Id, p.ProductCode, p.Name, p.CategoryId, c.Name AS Category,
                             p.Unit, p.MinQty, p.AvgPrice, p.ProductType, p.CreatedAt, p.IsActive
                      FROM dbo.Products p
                      LEFT JOIN dbo.ProductCategories c ON c.Id = p.CategoryId
                      ORDER BY p.Name ASC");
                items = fallback.AsList();
            }

            // Populate CreatedAt from dbo.Products if sp_GetProducts did not return it
            var missingIds = items.Where(p => p.Id.HasValue && (p.CreatedAt == null || string.IsNullOrWhiteSpace(p.CreatedAt.ToString())))
                                  .Select(p => p.Id!.Value)
                                  .ToList();

            if (missingIds.Count > 0)
            {
                try
                {
                    var dateRows = await conn.QueryAsync<dynamic>(
                        "SELECT Id, CreatedAt FROM dbo.Products WHERE Id IN @Ids",
                        new { Ids = missingIds });

                    var dateMap = new Dictionary<int, object?>();
                    foreach (var row in dateRows)
                    {
                        var dict = (IDictionary<string, object>)row;
                        int id = Convert.ToInt32(dict["Id"]);
                        dateMap[id] = dict.ContainsKey("CreatedAt") ? dict["CreatedAt"] : null;
                    }

                    foreach (var p in items)
                    {
                        if (p.Id.HasValue && dateMap.TryGetValue(p.Id.Value, out var dt) && dt != null)
                        {
                            p.CreatedAt = dt;
                        }
                    }
                }
                catch { }
            }

            return items;
        }

        public async Task<(int Id, string ProductCode)> SaveProductAsync(SaveProductDto product)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", product.Id);
            parameters.Add("@CategoryId", product.CategoryId);
            parameters.Add("@Name", product.Name);
            parameters.Add("@Unit", product.Unit);
            parameters.Add("@MinQty", product.MinQty);
            parameters.Add("@AvgPrice", product.AvgPrice);
            parameters.Add("@ProductType", product.ProductType);
            parameters.Add("@IsActive", product.IsActive);

            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "dbo.sp_SaveProduct",
                parameters,
                commandType: CommandType.StoredProcedure);

            if (result != null)
            {
                var dict = (IDictionary<string, object>)result;
                int id = Convert.ToInt32(dict["Id"]);
                string code = dict["ProductCode"]?.ToString() ?? "";
                return (id, code);
            }

            return (0, "");
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            using var conn = _context.CreateConnection();
            int rows = await conn.ExecuteAsync("DELETE FROM dbo.Products WHERE Id = @Id", new { Id = id });
            return rows > 0;
        }

        public async Task<bool> ToggleProductStatusAsync(int id, bool isActive)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@IsActive", isActive);

            int affected = await conn.ExecuteAsync(
                "dbo.sp_ToggleProductStatus",
                parameters,
                commandType: CommandType.StoredProcedure);

            return affected > 0;
        }
    }
}
