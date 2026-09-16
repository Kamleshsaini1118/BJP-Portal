using Microsoft.Data.SqlClient;
using StockPortalApp.Helpers;
using StockPortalApp.Models;
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

        public static async Task<List<ProductCategory>> GetCategoriesAsync()
        {
            var list = new List<ProductCategory>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetProductCategories", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new ProductCategory
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Prefix = reader.GetString(reader.GetOrdinal("Prefix")),
                });
            }

            return list;
        }

        /// <summary>
        /// Adds a new category. Mirrors the HTML's generateCategoryPrefix(): first 3 letters
        /// of the name, uppercased; if that prefix is already used, falls back to 2 letters + a number.
        /// </summary>
        public static async Task<ProductCategory> AddCategoryAsync(string name, IEnumerable<string> existingPrefixes)
        {
            string baseLetters = new string(name.Where(char.IsLetter).ToArray()).ToUpperInvariant();
            if (baseLetters.Length > 3) baseLetters = baseLetters[..3];
            if (baseLetters.Length == 0) baseLetters = "CAT";

            string prefix = baseLetters;
            var used = new HashSet<string>(existingPrefixes, StringComparer.OrdinalIgnoreCase);
            int n = 1;
            while (used.Contains(prefix))
            {
                n++;
                prefix = baseLetters[..Math.Min(2, baseLetters.Length)] + n;
            }

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddProductCategory", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Prefix", prefix);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new ProductCategory
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Prefix = reader.GetString(reader.GetOrdinal("Prefix")),
            };
        }

        public static async Task<List<Product>> GetProductsAsync()
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
                        CreatedAt = createdAtStr
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
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SaveProduct", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", (object?)product.Id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", product.CategoryId);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Unit", product.Unit);
            cmd.Parameters.AddWithValue("@MinQty", (object?)product.MinQty ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AvgPrice", (object?)product.AvgPrice ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("ProductCode")));
        }

        public static async Task DeleteProductAsync(int id)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("DELETE FROM dbo.Products WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
