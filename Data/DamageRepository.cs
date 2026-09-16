using Microsoft.Data.SqlClient;
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
    public static class DamageRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

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

        public static async Task<List<DamageRecord>> GetRecordsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
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
        }

        /// <summary>Returns (Id, DamageCode) for the newly created record.</summary>
        public static async Task<(int Id, string DamageCode)> AddRecordAsync(
            string? category, string itemName, decimal qty,
            string? reporteeName, string? reporteeNumber, string? reporteePosition, string? remark,
            string? imagePath = null)
        {
            string damageCode = $"DMG-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

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
            catch (SqlException ex) when (ex.Message.Contains("too many arguments", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("DamageCode", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("NULL", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("ImagePath", StringComparison.OrdinalIgnoreCase))
            {
                // Fall back to direct SQL execution if the stored procedure fails due to missing parameter or NULL constraint
            }

            try
            {
                string fallbackSqlWithImage = @"
                    INSERT INTO dbo.DamageRecords (DamageCode, Category, ItemName, Qty, ReporteeName, ReporteeNumber, ReporteePosition, Remark, ImagePath, DamageDate)
                    VALUES (@DamageCode, @Category, @ItemName, @Qty, @ReporteeName, @ReporteeNumber, @ReporteePosition, @Remark, @ImagePath, GETDATE());
                    SELECT SCOPE_IDENTITY() AS Id;";

                using var fallbackCmd = new SqlCommand(fallbackSqlWithImage, conn);
                fallbackCmd.Parameters.AddWithValue("@DamageCode", damageCode);
                fallbackCmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
                fallbackCmd.Parameters.AddWithValue("@ItemName", itemName);
                fallbackCmd.Parameters.AddWithValue("@Qty", qty);
                fallbackCmd.Parameters.AddWithValue("@ReporteeName", (object?)reporteeName ?? DBNull.Value);
                fallbackCmd.Parameters.AddWithValue("@ReporteeNumber", (object?)reporteeNumber ?? DBNull.Value);
                fallbackCmd.Parameters.AddWithValue("@ReporteePosition", (object?)reporteePosition ?? DBNull.Value);
                fallbackCmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);
                fallbackCmd.Parameters.AddWithValue("@ImagePath", (object?)imagePath ?? DBNull.Value);

                var res = await fallbackCmd.ExecuteScalarAsync();
                int newId = Convert.ToInt32(res);
                return (newId, damageCode);
            }
            catch (SqlException)
            {
                string fallbackSqlWithoutImage = @"
                    INSERT INTO dbo.DamageRecords (DamageCode, Category, ItemName, Qty, ReporteeName, ReporteeNumber, ReporteePosition, Remark, DamageDate)
                    VALUES (@DamageCode, @Category, @ItemName, @Qty, @ReporteeName, @ReporteeNumber, @ReporteePosition, @Remark, GETDATE());
                    SELECT SCOPE_IDENTITY() AS Id;";

                using var fallbackCmdNoImage = new SqlCommand(fallbackSqlWithoutImage, conn);
                fallbackCmdNoImage.Parameters.AddWithValue("@DamageCode", damageCode);
                fallbackCmdNoImage.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
                fallbackCmdNoImage.Parameters.AddWithValue("@ItemName", itemName);
                fallbackCmdNoImage.Parameters.AddWithValue("@Qty", qty);
                fallbackCmdNoImage.Parameters.AddWithValue("@ReporteeName", (object?)reporteeName ?? DBNull.Value);
                fallbackCmdNoImage.Parameters.AddWithValue("@ReporteeNumber", (object?)reporteeNumber ?? DBNull.Value);
                fallbackCmdNoImage.Parameters.AddWithValue("@ReporteePosition", (object?)reporteePosition ?? DBNull.Value);
                fallbackCmdNoImage.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);

                var res = await fallbackCmdNoImage.ExecuteScalarAsync();
                int newId = Convert.ToInt32(res);
                return (newId, damageCode);
            }
        }
    }
}
