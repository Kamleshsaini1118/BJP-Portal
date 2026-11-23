using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.Utilities;

namespace StockPortalApp.Data
{
    public static class UtilityExpenseRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"]?.ConnectionString ?? string.Empty;

        // Fallback in-memory list if SQL table/stored procedure is not ready yet
        private static readonly List<UtilityExpense> InMemExpenses = new();
        private static readonly object SyncLock = new();

        public static async Task<List<UtilityExpense>> GetExpensesAsync(string? searchQuery = null)
        {
            // API Call
            var url = "utilities/expenses" + (!string.IsNullOrWhiteSpace(searchQuery) ? $"?searchQuery={Uri.EscapeDataString(searchQuery)}" : "");
            var response = await ApiClient.GetAsync<List<UtilityExpense>>(url);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                var list = new List<UtilityExpense>();
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("dbo.sp_GetUtilityExpenses", conn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@SearchQuery", (object?)searchQuery ?? DBNull.Value);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new UtilityExpense
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "Plumbing" : reader.GetString(reader.GetOrdinal("Category")),
                                    Location = reader.GetString(reader.GetOrdinal("Location")),
                                    Amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Amount")),
                                    ExpenseDate = reader.IsDBNull(reader.GetOrdinal("ExpenseDate")) ? DateTime.Today : reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                    BillNo = reader.IsDBNull(reader.GetOrdinal("BillNo")) ? null : reader.GetString(reader.GetOrdinal("BillNo")),
                                    BillImagePath = reader.IsDBNull(reader.GetOrdinal("BillImagePath")) ? null : reader.GetString(reader.GetOrdinal("BillImagePath")),
                                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                                });
                            }
                        }
                    }
                }
                return list;
            }
            catch
            {
                lock (SyncLock)
                {
                    var query = InMemExpenses.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        var s = searchQuery.Trim().ToLowerInvariant();
                        query = query.Where(e => (e.Category?.ToLowerInvariant().Contains(s) ?? false)
                                              || (e.Location?.ToLowerInvariant().Contains(s) ?? false)
                                              || (e.Description?.ToLowerInvariant().Contains(s) ?? false)
                                              || (e.BillNo?.ToLowerInvariant().Contains(s) ?? false));
                    }
                    return query.OrderByDescending(e => e.ExpenseDate).ToList();
                }
            }
            */
            return new List<UtilityExpense>();
        }

        public static async Task<int> SaveExpenseAsync(UtilityExpense expense)
        {
            // API Call
            var dto = new SaveUtilityExpenseDto
            {
                Id = expense.Id,
                Category = expense.Category,
                Location = expense.Location,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                Description = expense.Description,
                BillNo = expense.BillNo,
                BillImagePath = expense.BillImagePath
            };

            var response = await ApiClient.PostAsync<int>("utilities/expenses", dto);
            if (response.Success)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("dbo.sp_SaveUtilityExpense", conn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@Id", (object?)expense.Id ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Category", expense.Category ?? "Plumbing");
                        cmd.Parameters.AddWithValue("@Location", expense.Location);
                        cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                        cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate.Date);
                        cmd.Parameters.AddWithValue("@Description", (object?)expense.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BillNo", (object?)expense.BillNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BillImagePath", (object?)expense.BillImagePath ?? DBNull.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    if (expense.Id.HasValue && expense.Id.Value > 0)
                    {
                        var existing = InMemExpenses.FirstOrDefault(e => e.Id == expense.Id.Value);
                        if (existing != null) InMemExpenses.Remove(existing);
                        InMemExpenses.Add(expense);
                        return expense.Id.Value;
                    }
                    else
                    {
                        int nextId = (InMemExpenses.Count > 0 ? InMemExpenses.Max(e => e.Id ?? 0) : 0) + 1;
                        expense.Id = nextId;
                        InMemExpenses.Add(expense);
                        return nextId;
                    }
                }
            }
            */
            return 0;
        }

        public static async Task<bool> DeleteExpenseAsync(int id)
        {
            // API Call
            var response = await ApiClient.DeleteAsync<bool>($"utilities/expenses/{id}");
            if (response.Success)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("dbo.sp_DeleteUtilityExpense", conn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int affected = await cmd.ExecuteNonQueryAsync();
                        return affected > 0;
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    int removed = InMemExpenses.RemoveAll(e => e.Id == id);
                    return removed > 0;
                }
            }
            */
            return false;
        }
    }
}
