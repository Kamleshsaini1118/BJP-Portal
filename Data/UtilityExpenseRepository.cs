using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StockPortalApp.Models;

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
                // Fallback to in-memory list if SQL procedure fails or table isn't created yet
                lock (SyncLock)
                {
                    var query = InMemExpenses.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        string term = searchQuery.Trim().ToLowerInvariant();
                        query = query.Where(e => e.Category.ToLowerInvariant().Contains(term)
                                              || e.Location.ToLowerInvariant().Contains(term)
                                              || (e.Description != null && e.Description.ToLowerInvariant().Contains(term))
                                              || (e.BillNo != null && e.BillNo.ToLowerInvariant().Contains(term)));
                    }
                    return query.OrderByDescending(e => e.ExpenseDate).ToList();
                }
            }
        }

        public static async Task SaveExpenseAsync(UtilityExpense expense)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("dbo.sp_AddUtilityExpense", conn) { CommandType = CommandType.StoredProcedure })
                    {
                        cmd.Parameters.AddWithValue("@Category", expense.Category);
                        cmd.Parameters.AddWithValue("@Location", expense.Location);
                        cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                        cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate.Date);
                        cmd.Parameters.AddWithValue("@Description", (object?)expense.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BillNo", (object?)expense.BillNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BillImagePath", (object?)expense.BillImagePath ?? DBNull.Value);

                        var res = await cmd.ExecuteScalarAsync();
                        if (res != null && Convert.ToInt32(res) > 0)
                        {
                            expense.Id = Convert.ToInt32(res);
                        }
                    }
                }
            }
            catch
            {
                // Fallback to in-memory cache
                lock (SyncLock)
                {
                    if (expense.Id <= 0)
                    {
                        int nextId = InMemExpenses.Count > 0 ? InMemExpenses.Max(e => e.Id) + 1 : 1;
                        expense.Id = nextId;
                        InMemExpenses.Add(expense);
                    }
                    else
                    {
                        var existing = InMemExpenses.FirstOrDefault(e => e.Id == expense.Id);
                        if (existing != null)
                        {
                            existing.Category = expense.Category;
                            existing.Location = expense.Location;
                            existing.Amount = expense.Amount;
                            existing.ExpenseDate = expense.ExpenseDate;
                            existing.Description = expense.Description;
                            existing.BillNo = expense.BillNo;
                            existing.BillImagePath = expense.BillImagePath;
                        }
                    }
                }
            }
        }

        public static async Task DeleteExpenseAsync(int expenseId)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string deleteSql = "DELETE FROM [dbo].[UtilityExpenses] WHERE [Id] = @Id";
                    using (var cmd = new SqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", expenseId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    InMemExpenses.RemoveAll(e => e.Id == expenseId);
                }
            }
        }
    }
}
