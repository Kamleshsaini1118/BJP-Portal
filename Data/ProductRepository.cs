using Microsoft.Data.SqlClient;
using StockPortalApp.Helpers;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.Products;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Data
{
    public static class ProductRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        private static string FormatCreatedAt(object? val)
        {
            if (val == null) return "—";
            if (val is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var s = je.GetString();
                    if (DateTime.TryParse(s, out var parsedDate)) return parsedDate.ToString("dd MMM yyyy");
                    return string.IsNullOrWhiteSpace(s) ? "—" : s;
                }
                if (je.TryGetDateTime(out var dtVal))
                {
                    return dtVal.ToString("dd MMM yyyy");
                }
                return je.ToString();
            }
            if (val is DateTime dateVal) return dateVal.ToString("dd MMM yyyy");
            var str = val.ToString()?.Trim() ?? "";
            if (DateTime.TryParse(str, out var dtFromStr)) return dtFromStr.ToString("dd MMM yyyy");
            return string.IsNullOrWhiteSpace(str) ? "—" : str;
        }

        public static async Task<List<ProductCategory>> GetCategoriesAsync(string? searchQuery = null, string statusFilter = "ALL")
        {
            try
            {
                var query = $"products/categories?statusFilter={Uri.EscapeDataString(statusFilter)}";
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query += $"&searchQuery={Uri.EscapeDataString(searchQuery)}";
                }

                var response = await ApiClient.GetAsync<List<ProductCategoryDto>>(query);
                if (response.Success && response.Data != null && response.Data.Count > 0)
                {
                    return response.Data.Select(d => new ProductCategory
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Prefix = d.Prefix,
                        IsActive = d.IsActive ?? true,
                        ProductCount = d.ProductCount ?? 0
                    }).ToList();
                }
            }
            catch { }

            // Fallback to direct SQL
            return await GetCategoriesDirectSqlAsync(searchQuery, statusFilter);
        }

        public static async Task<List<ProductCategory>> GetCategoriesDirectSqlAsync(string? searchQuery = null, string statusFilter = "ALL")
        {
            var list = new List<ProductCategory>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetProductCategories", conn) { CommandType = CommandType.StoredProcedure };
            if (!string.IsNullOrEmpty(searchQuery))
            {
                cmd.Parameters.AddWithValue("@SearchQuery", searchQuery);
            }
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "ALL")
            {
                cmd.Parameters.AddWithValue("@StatusFilter", statusFilter);
            }

            using var reader = await cmd.ExecuteReaderAsync();

            int idOrd = reader.GetOrdinal("Id");
            int nameOrd = reader.GetOrdinal("Name");
            int prefixOrd = reader.GetOrdinal("Prefix");
            int activeOrd = -1;
            int countOrd = -1;
            try { activeOrd = reader.GetOrdinal("IsActive"); } catch { }
            try { countOrd = reader.GetOrdinal("ProductCount"); } catch { }

            while (await reader.ReadAsync())
            {
                list.Add(new ProductCategory
                {
                    Id = reader.GetInt32(idOrd),
                    Name = reader.GetString(nameOrd),
                    Prefix = reader.GetString(prefixOrd),
                    IsActive = activeOrd >= 0 && !reader.IsDBNull(activeOrd) ? reader.GetBoolean(activeOrd) : true,
                    ProductCount = countOrd >= 0 && !reader.IsDBNull(countOrd) ? reader.GetInt32(countOrd) : 0,
                });
            }

            return list;
        }

        /// <summary>
        /// Adds a new category.
        /// </summary>
        public static async Task<ProductCategory> AddCategoryAsync(string name, IEnumerable<string> existingPrefixes, string? customPrefix = null, bool isActive = true)
        {
            // API Call
            var payload = new CreateCategoryDto
            {
                Name = name,
                CustomPrefix = customPrefix,
                IsActive = isActive
            };

            var response = await ApiClient.PostAsync<ProductCategory>("products/categories", payload);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            string prefix;
            if (!string.IsNullOrWhiteSpace(customPrefix))
            {
                prefix = customPrefix.Trim().ToUpperInvariant();
            }
            else
            {
                string baseLetters = new string(name.Where(char.IsLetter).ToArray()).ToUpperInvariant();
                if (baseLetters.Length > 3) baseLetters = baseLetters[..3];
                if (baseLetters.Length == 0) baseLetters = "CAT";

                prefix = baseLetters;
                var used = new HashSet<string>(existingPrefixes, StringComparer.OrdinalIgnoreCase);
                int n = 1;
                while (used.Contains(prefix))
                {
                    n++;
                    prefix = baseLetters[..Math.Min(2, baseLetters.Length)] + n;
                }
            }

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddProductCategory", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Prefix", prefix);
            cmd.Parameters.AddWithValue("@IsActive", isActive);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            int idOrd = reader.GetOrdinal("Id");
            int nameOrd = reader.GetOrdinal("Name");
            int prefixOrd = reader.GetOrdinal("Prefix");
            int activeOrd = -1;
            try { activeOrd = reader.GetOrdinal("IsActive"); } catch { }

            return new ProductCategory
            {
                Id = reader.GetInt32(idOrd),
                Name = reader.GetString(nameOrd),
                Prefix = reader.GetString(prefixOrd),
                IsActive = activeOrd >= 0 && !reader.IsDBNull(activeOrd) ? reader.GetBoolean(activeOrd) : isActive,
            };
            */
            return new ProductCategory { Name = name, Prefix = customPrefix ?? "CAT", IsActive = isActive };
        }

        public static async Task ToggleCategoryStatusAsync(int id, bool isActive)
        {
            // API Call
            await ApiClient.PatchAsync<bool>($"products/categories/{id}/status", new ToggleStatusDto { IsActive = isActive });

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ToggleCategoryStatus", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@IsActive", isActive);

            await cmd.ExecuteNonQueryAsync();
            */
        }

        public static async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                // API Call
                var response = await ApiClient.GetAsync<List<ProductDto>>("products");
                if (response.Success && response.Data != null && response.Data.Count > 0)
                {
                    return response.Data.Select(d => new Product
                    {
                        Id = d.Id,
                        ProductCode = d.ProductCode,
                        Name = d.Name,
                        CategoryId = d.CategoryId,
                        Category = d.Category,
                        Unit = d.Unit,
                        MinQty = d.MinQty,
                        AvgPrice = d.AvgPrice,
                        ProductType = d.ProductType ?? "Moving Asset",
                        CreatedAt = FormatCreatedAt(d.CreatedAt),
                        IsActive = d.IsActive
                    }).ToList();
                }
            }
            catch { }

            // Fallback to direct SQL
            return await GetProductsDirectSqlAsync();
        }

        public static async Task<List<Product>> GetProductsDirectSqlAsync()
        {
            var list = new List<Product>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using (var cmd = new SqlCommand("dbo.sp_GetProducts", conn) { CommandType = CommandType.StoredProcedure })
            {
                using var reader = await cmd.ExecuteReaderAsync();

                int createdAtOrdinal = -1;
                string[] possibleCols = { "CreatedAt", "Created_At", "CreatedDate", "Created_Date", "CreatedOn", "DateCreated", "CreatedDateTime", "InsertDate", "EntryDate", "Date" };
                foreach (var col in possibleCols)
                {
                    try
                    {
                        createdAtOrdinal = reader.GetOrdinal(col);
                        if (createdAtOrdinal >= 0) break;
                    }
                    catch { }
                }

                int productTypeOrd = -1;
                try { productTypeOrd = reader.GetOrdinal("ProductType"); } catch { }

                while (await reader.ReadAsync())
                {
                    string createdAtStr = "—";
                    if (createdAtOrdinal >= 0 && !reader.IsDBNull(createdAtOrdinal))
                    {
                        var val = reader.GetValue(createdAtOrdinal);
                        if (val is DateTime dt)
                        {
                            createdAtStr = dt.ToString("dd MMM yyyy");
                        }
                        else
                        {
                            var s = val.ToString() ?? "";
                            if (DateTime.TryParse(s, out var parsedDt))
                            {
                                createdAtStr = parsedDt.ToString("dd MMM yyyy");
                            }
                            else if (!string.IsNullOrWhiteSpace(s))
                            {
                                createdAtStr = s;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(createdAtStr))
                    {
                        createdAtStr = "—";
                    }

                    list.Add(new Product
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        ProductCode = reader.GetString(reader.GetOrdinal("ProductCode")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                        Category = reader.GetString(reader.GetOrdinal("Category")),
                        Unit = reader.GetString(reader.GetOrdinal("Unit")),
                        MinQty = reader.IsDBNull(reader.GetOrdinal("MinQty")) ? null : reader.GetDecimal(reader.GetOrdinal("MinQty")),
                        AvgPrice = reader.IsDBNull(reader.GetOrdinal("AvgPrice")) ? null : reader.GetDecimal(reader.GetOrdinal("AvgPrice")),
                        ProductType = productTypeOrd >= 0 && !reader.IsDBNull(productTypeOrd) ? reader.GetString(productTypeOrd) : "",
                        CreatedAt = createdAtStr,
                        IsActive = !reader.IsDBNull(reader.GetOrdinal("IsActive")) && reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    });
                }
            }

            var unpopulatedProducts = list.Where(p => string.IsNullOrWhiteSpace(p.CreatedAt) || p.CreatedAt == "—").ToList();
            if (unpopulatedProducts.Count > 0)
            {
                try
                {
                    var ids = unpopulatedProducts.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToList();
                    if (ids.Count > 0)
                    {
                        string idList = string.Join(",", ids);
                        string querySql = $"SELECT Id, CreatedAt FROM dbo.Products WHERE Id IN ({idList})";
                        using var queryCmd = new SqlCommand(querySql, conn);
                        using var queryReader = await queryCmd.ExecuteReaderAsync();
                        var dateMap = new Dictionary<int, string>();
                        while (await queryReader.ReadAsync())
                        {
                            int id = queryReader.GetInt32(0);
                            if (!queryReader.IsDBNull(1))
                            {
                                var val = queryReader.GetValue(1);
                                if (val is DateTime dt) dateMap[id] = dt.ToString("dd MMM yyyy");
                                else if (DateTime.TryParse(val.ToString(), out var pDt)) dateMap[id] = pDt.ToString("dd MMM yyyy");
                            }
                        }

                        foreach (var p in list)
                        {
                            if (p.Id.HasValue && dateMap.TryGetValue(p.Id.Value, out var realDate))
                            {
                                p.CreatedAt = realDate;
                            }
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        /// <summary>Inserts (product.Id == null) or updates. Returns (Id, ProductCode).</summary>
        public static async Task<(int Id, string ProductCode)> SaveProductAsync(Product product)
        {
            // API Call
            var dto = new SaveProductDto
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Category = product.Category,
                Unit = product.Unit,
                MinQty = product.MinQty,
                AvgPrice = product.AvgPrice,
                ProductType = product.ProductType,
                IsActive = product.IsActive
            };

            var response = await ApiClient.PostAsync<Dictionary<string, object>>("products", dto);
            if (response.Success && response.Data != null)
            {
                int id = 0;
                string code = string.Empty;
                if (response.Data.TryGetValue("id", out var idVal) || response.Data.TryGetValue("Id", out idVal))
                {
                    int.TryParse(idVal?.ToString(), out id);
                }
                if (response.Data.TryGetValue("productCode", out var codeVal) || response.Data.TryGetValue("ProductCode", out codeVal))
                {
                    code = codeVal?.ToString() ?? string.Empty;
                }
                return (id, code);
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SaveProduct", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", (object?)product.Id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Unit", product.Unit);
            cmd.Parameters.AddWithValue("@MinQty", (object?)product.MinQty ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AvgPrice", (object?)product.AvgPrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductType", (object?)product.ProductType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", product.IsActive);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("ProductCode")));
            */
            return (0, string.Empty);
        }

        public static async Task DeleteProductAsync(int id)
        {
            // API Call
            await ApiClient.DeleteAsync<bool>($"products/{id}");

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("DELETE FROM dbo.Products WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
            */
        }

        public static async Task ToggleProductStatusAsync(int id, bool isActive)
        {
            // API Call
            await ApiClient.PatchAsync<bool>($"products/{id}/status", new ToggleStatusDto { IsActive = isActive });

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ToggleProductStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@IsActive", isActive);

            await cmd.ExecuteNonQueryAsync();
            */
        }
    }
}
