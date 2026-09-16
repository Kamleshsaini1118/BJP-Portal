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
    public static class UtilityReminderRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"]?.ConnectionString ?? string.Empty;

        // Fallback in-memory cache if SQL table isn't ready
        private static readonly List<UtilityReminder> InMemReminders = new();
        private static readonly object SyncLock = new();

        public static async Task<List<UtilityReminder>> GetRemindersAsync(string? searchQuery = null)
        {
            try
            {
                var list = new List<UtilityReminder>();
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"SELECT [Id], [UtilityName], [DueDay], [RemindDaysBefore], [Remarks], [CreatedAt] 
                                   FROM [dbo].[UtilityReminders]";

                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        sql += @" WHERE [UtilityName] LIKE @Search 
                                    OR [Remarks] LIKE @Search";
                    }

                    sql += " ORDER BY [DueDay] ASC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(searchQuery))
                        {
                            cmd.Parameters.AddWithValue("@Search", $"%{searchQuery.Trim()}%");
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int id = reader.GetInt32(reader.GetOrdinal("Id"));
                                string name = reader.GetString(reader.GetOrdinal("UtilityName"));
                                int dueDay = reader.GetInt32(reader.GetOrdinal("DueDay"));
                                string remindStr = reader.IsDBNull(reader.GetOrdinal("RemindDaysBefore")) 
                                    ? string.Empty 
                                    : reader.GetString(reader.GetOrdinal("RemindDaysBefore"));
                                string? remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) 
                                    ? null 
                                    : reader.GetString(reader.GetOrdinal("Remarks"));
                                DateTime createdAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) 
                                    ? DateTime.Now 
                                    : reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

                                var remindList = string.IsNullOrWhiteSpace(remindStr)
                                    ? new List<int>()
                                    : remindStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => int.TryParse(s.Trim(), out int n) ? n : 0)
                                               .Where(n => n > 0)
                                               .ToList();

                                list.Add(new UtilityReminder
                                {
                                    Id = id,
                                    UtilityName = name,
                                    DueDay = dueDay,
                                    RemindDaysBefore = remindList,
                                    Remarks = remarks,
                                    CreatedAt = createdAt
                                });
                            }
                        }
                    }
                }
                return list;
            }
            catch
            {
                // Fallback to in-memory cache if SQL database fails
                lock (SyncLock)
                {
                    var query = InMemReminders.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        string term = searchQuery.Trim().ToLowerInvariant();
                        query = query.Where(r => r.UtilityName.ToLowerInvariant().Contains(term)
                                              || r.DueDayDisplay.ToLowerInvariant().Contains(term)
                                              || r.RemindBeforeDisplay.ToLowerInvariant().Contains(term)
                                              || (r.Remarks != null && r.Remarks.ToLowerInvariant().Contains(term)));
                    }
                    return query.OrderBy(r => r.DueDay).ToList();
                }
            }
        }

        public static async Task SaveReminderAsync(UtilityReminder reminder)
        {
            try
            {
                string remindDaysStr = reminder.RemindDaysBefore != null && reminder.RemindDaysBefore.Count > 0
                    ? string.Join(",", reminder.RemindDaysBefore.OrderBy(x => x))
                    : string.Empty;

                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    if (reminder.Id <= 0)
                    {
                        string insertSql = @"INSERT INTO [dbo].[UtilityReminders] 
                                             ([UtilityName], [DueDay], [RemindDaysBefore], [Remarks], [CreatedAt]) 
                                             VALUES (@UtilityName, @DueDay, @RemindDaysBefore, @Remarks, GETDATE());
                                             SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(insertSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@UtilityName", reminder.UtilityName);
                            cmd.Parameters.AddWithValue("@DueDay", reminder.DueDay);
                            cmd.Parameters.AddWithValue("@RemindDaysBefore", remindDaysStr);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)reminder.Remarks ?? DBNull.Value);

                            var newIdObj = await cmd.ExecuteScalarAsync();
                            if (newIdObj != null && Convert.ToInt32(newIdObj) > 0)
                            {
                                reminder.Id = Convert.ToInt32(newIdObj);
                            }
                        }
                    }
                    else
                    {
                        string updateSql = @"UPDATE [dbo].[UtilityReminders] 
                                             SET [UtilityName] = @UtilityName,
                                                 [DueDay] = @DueDay,
                                                 [RemindDaysBefore] = @RemindDaysBefore,
                                                 [Remarks] = @Remarks
                                             WHERE [Id] = @Id";

                        using (var cmd = new SqlCommand(updateSql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", reminder.Id);
                            cmd.Parameters.AddWithValue("@UtilityName", reminder.UtilityName);
                            cmd.Parameters.AddWithValue("@DueDay", reminder.DueDay);
                            cmd.Parameters.AddWithValue("@RemindDaysBefore", remindDaysStr);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)reminder.Remarks ?? DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch
            {
                // Fallback to in-memory cache
                lock (SyncLock)
                {
                    if (reminder.Id <= 0)
                    {
                        int nextId = InMemReminders.Count > 0 ? InMemReminders.Max(r => r.Id) + 1 : 1;
                        reminder.Id = nextId;
                        InMemReminders.Add(reminder);
                    }
                    else
                    {
                        var existing = InMemReminders.FirstOrDefault(r => r.Id == reminder.Id);
                        if (existing != null)
                        {
                            existing.UtilityName = reminder.UtilityName;
                            existing.DueDay = reminder.DueDay;
                            existing.RemindDaysBefore = new List<int>(reminder.RemindDaysBefore);
                            existing.Remarks = reminder.Remarks;
                        }
                    }
                }
            }
        }

        public static async Task DeleteReminderAsync(int reminderId)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string deleteSql = "DELETE FROM [dbo].[UtilityReminders] WHERE [Id] = @Id";
                    using (var cmd = new SqlCommand(deleteSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", reminderId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    InMemReminders.RemoveAll(r => r.Id == reminderId);
                }
            }
        }
    }
}