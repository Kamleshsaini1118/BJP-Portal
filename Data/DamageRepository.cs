using Microsoft.Data.SqlClient;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.Damage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Data
{
    public static class DamageRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<DamageRecord>> GetRecordsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            // API Call
            var query = "damage-records";
            var qParams = new List<string>();
            if (dateFrom.HasValue) qParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue) qParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
            if (qParams.Count > 0) query += "?" + string.Join("&", qParams);

            var response = await ApiClient.GetAsync<List<DamageRecord>>(query);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            var list = new List<DamageRecord>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await EnsureImagePathColumnExistsAsync(conn);

            try
            {
                using var cmd = new SqlCommand("dbo.sp_GetDamageRecords", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                int imagePathOrdinal = -1;
                try { imagePathOrdinal = reader.GetOrdinal("ImagePath"); } catch { }

                if (imagePathOrdinal >= 0)
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new DamageRecord
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            DamageCode = reader.GetString(reader.GetOrdinal("DamageCode")),
                            ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                            Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                            Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                            ReporteeName = reader.IsDBNull(reader.GetOrdinal("ReporteeName")) ? null : reader.GetString(reader.GetOrdinal("ReporteeName")),
                            ReporteeNumber = reader.IsDBNull(reader.GetOrdinal("ReporteeNumber")) ? null : reader.GetString(reader.GetOrdinal("ReporteeNumber")),
                            ReporteePosition = reader.IsDBNull(reader.GetOrdinal("ReporteePosition")) ? null : reader.GetString(reader.GetOrdinal("ReporteePosition")),
                            DamageDate = reader.GetDateTime(reader.GetOrdinal("DamageDate")),
                            Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                            ImagePath = reader.IsDBNull(imagePathOrdinal) ? null : reader.GetString(imagePathOrdinal),
                        });
                    }
                    return list;
                }
            }
            catch { }

            // Direct SQL fallback if sp_GetDamageRecords does not return ImagePath column
            list.Clear();
            string sqlFallback = @"
                SELECT Id, DamageCode, Category, ItemName, Qty, ReporteeName, ReporteeNumber, ReporteePosition, DamageDate, Remark, ImagePath
                FROM dbo.DamageRecords
                WHERE (@DateFrom IS NULL OR DamageDate >= @DateFrom)
                  AND (@DateTo IS NULL OR DamageDate <= @DateTo)
                ORDER BY Id DESC";

            using var fallbackCmd = new SqlCommand(sqlFallback, conn);
            fallbackCmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
            fallbackCmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);

            using var reader2 = await fallbackCmd.ExecuteReaderAsync();
            int imgOrd = -1;
            try { imgOrd = reader2.GetOrdinal("ImagePath"); } catch { }

            while (await reader2.ReadAsync())
            {
                list.Add(new DamageRecord
                {
                    Id = reader2.GetInt32(reader2.GetOrdinal("Id")),
                    DamageCode = reader2.GetString(reader2.GetOrdinal("DamageCode")),
                    ItemName = reader2.GetString(reader2.GetOrdinal("ItemName")),
                    Category = reader2.IsDBNull(reader2.GetOrdinal("Category")) ? null : reader2.GetString(reader2.GetOrdinal("Category")),
                    Qty = reader2.GetDecimal(reader2.GetOrdinal("Qty")),
                    ReporteeName = reader2.IsDBNull(reader2.GetOrdinal("ReporteeName")) ? null : reader2.GetString(reader2.GetOrdinal("ReporteeName")),
                    ReporteeNumber = reader2.IsDBNull(reader2.GetOrdinal("ReporteeNumber")) ? null : reader2.GetString(reader2.GetOrdinal("ReporteeNumber")),
                    ReporteePosition = reader2.IsDBNull(reader2.GetOrdinal("ReporteePosition")) ? null : reader2.GetString(reader2.GetOrdinal("ReporteePosition")),
                    DamageDate = reader2.GetDateTime(reader2.GetOrdinal("DamageDate")),
                    Remark = reader2.IsDBNull(reader2.GetOrdinal("Remark")) ? null : reader2.GetString(reader2.GetOrdinal("Remark")),
                    ImagePath = (imgOrd >= 0 && !reader2.IsDBNull(imgOrd)) ? reader2.GetString(imgOrd) : null,
                });
            }

            return list;
            */
            return new List<DamageRecord>();
        }

        public static async Task<DamageRecord?> GetRecordByIdAsync(int id)
        {
            // API Call
            var response = await ApiClient.GetAsync<DamageRecordDto>($"damage-records/{id}");
            if (response.Success && response.Data != null)
            {
                var d = response.Data;
                return new DamageRecord
                {
                    Id = d.Id,
                    DamageCode = d.DamageCode,
                    ItemName = d.ItemName,
                    Category = d.Category,
                    Qty = d.Qty,
                    ReporteeName = d.ReporteeName,
                    ReporteeNumber = d.ReporteeNumber,
                    ReporteePosition = d.ReporteePosition,
                    DamageDate = d.DamageDate,
                    Remark = d.Remark,
                    ImagePath = d.ImagePath
                };
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
                SELECT Id, DamageCode, Category, ItemName, Qty, ReporteeName, 
                       ReporteeNumber, ReporteePosition, DamageDate, Remark, ImagePath
                FROM dbo.DamageRecords WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int imgOrd = -1;
                try { imgOrd = reader.GetOrdinal("ImagePath"); } catch { }
                return new DamageRecord
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    DamageCode = reader.GetString(reader.GetOrdinal("DamageCode")),
                    ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                    ReporteeName = reader.IsDBNull(reader.GetOrdinal("ReporteeName")) ? null : reader.GetString(reader.GetOrdinal("ReporteeName")),
                    ReporteeNumber = reader.IsDBNull(reader.GetOrdinal("ReporteeNumber")) ? null : reader.GetString(reader.GetOrdinal("ReporteeNumber")),
                    ReporteePosition = reader.IsDBNull(reader.GetOrdinal("ReporteePosition")) ? null : reader.GetString(reader.GetOrdinal("ReporteePosition")),
                    DamageDate = reader.GetDateTime(reader.GetOrdinal("DamageDate")),
                    Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                    ImagePath = (imgOrd >= 0 && !reader.IsDBNull(imgOrd)) ? reader.GetString(imgOrd) : null
                };
            }
            */
            return null;
        }

        private static async Task EnsureImagePathColumnExistsAsync(SqlConnection conn)
        {
            try
            {
                string sql = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.columns 
                        WHERE object_id = OBJECT_ID('dbo.DamageRecords') AND name = 'ImagePath'
                    )
                    BEGIN
                        ALTER TABLE dbo.DamageRecords ADD ImagePath NVARCHAR(MAX) NULL;
                    END";
                using var cmd = new SqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }

        /// <summary>Returns (Id, DamageCode) for the newly created record.</summary>
        public static async Task<(int Id, string DamageCode)> AddRecordAsync(
            string? category, string itemName, decimal qty,
            string? reporteeName, string? reporteeNumber, string? reporteePosition, string? remark,
            string? imagePath = null)
        {
            // API Call
            var dto = new
            {
                Category = category,
                ItemName = itemName,
                Qty = qty,
                ReporteeName = reporteeName,
                ReporteeNumber = reporteeNumber,
                ReporteePosition = reporteePosition,
                Remark = remark,
                ImagePath = imagePath
            };

            var response = await ApiClient.PostAsync<Dictionary<string, object>>("damage-records", dto);
            if (response.Success && response.Data != null)
            {
                int id = 0;
                string code = string.Empty;
                if (response.Data.TryGetValue("id", out var idVal) || response.Data.TryGetValue("Id", out idVal))
                {
                    int.TryParse(idVal?.ToString(), out id);
                }
                if (response.Data.TryGetValue("damageCode", out var codeVal) || response.Data.TryGetValue("DamageCode", out codeVal))
                {
                    code = codeVal?.ToString() ?? string.Empty;
                }
                return (id, code);
            }

            throw new InvalidOperationException(response.Message ?? "Failed to save damage record.");

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await EnsureImagePathColumnExistsAsync(conn);

            try
            {
                using var cmd = new SqlCommand("dbo.sp_AddDamageRecord", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@DamageCode", damageCode);
                cmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ItemName", itemName);
                cmd.Parameters.AddWithValue("@Qty", qty);
                cmd.Parameters.AddWithValue("@ReporteeName", (object?)reporteeName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReporteeNumber", (object?)reporteeNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReporteePosition", (object?)reporteePosition ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ImagePath", (object?)imagePath ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("Id"));
                    string returnedCode = reader.IsDBNull(reader.GetOrdinal("DamageCode")) ? damageCode : reader.GetString(reader.GetOrdinal("DamageCode"));
                    if (string.IsNullOrWhiteSpace(returnedCode)) returnedCode = damageCode;
                    return (id, returnedCode);
                }
            }
            catch { }

            return (0, damageCode);
            return (0, string.Empty);
            */
        }
    }
}
