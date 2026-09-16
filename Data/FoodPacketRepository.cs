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
    public static class FoodPacketRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<FoodPacketBatch>> GetBatchesAsync()
        {
            var list = new List<FoodPacketBatch>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetFoodPacketBatches", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int meetingCampOrdinal = GetOrdinalSafe(reader, "MeetingCampaign");
                list.Add(new FoodPacketBatch
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    BatchCode = reader.GetString(reader.GetOrdinal("BatchCode")),
                    ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                    MealFor = reader.IsDBNull(reader.GetOrdinal("MealFor")) ? null : reader.GetString(reader.GetOrdinal("MealFor")),
                    VendorName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? null : reader.GetString(reader.GetOrdinal("VendorName")),
                    MeetingCampaign = (meetingCampOrdinal >= 0 && !reader.IsDBNull(meetingCampOrdinal)) ? reader.GetString(meetingCampOrdinal) : null,
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                    AvailableQty = reader.GetDecimal(reader.GetOrdinal("AvailableQty")),
                    DispatchedQty = reader.GetDecimal(reader.GetOrdinal("DispatchedQty")),
                    BatchDate = reader.GetDateTime(reader.GetOrdinal("BatchDate")),
                    DispatchCount = reader.GetInt32(reader.GetOrdinal("DispatchCount")),
                });
            }

            return list;
        }

        private static int GetOrdinalSafe(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>Adds one batch (called once per item row). Returns (Id, BatchCode).</summary>
        public static async Task<(int Id, string BatchCode)> AddBatchAsync(
            string itemName, string? mealFor, string? vendorName, string? meetingCampaign, decimal qty,
            DateTime batchDate, string? receiverName, string? receiverNumber)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddFoodPacketBatch", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ItemName", itemName);
            cmd.Parameters.AddWithValue("@MealFor", (object?)mealFor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorName", (object?)vendorName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MeetingCampaign", (object?)meetingCampaign ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Qty", qty);
            cmd.Parameters.AddWithValue("@BatchDate", batchDate.Date);
            cmd.Parameters.AddWithValue("@ReceiverName", (object?)receiverName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiverNumber", (object?)receiverNumber ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("BatchCode")));
        }

        /// <summary>Dispatches a quantity from a batch. Throws SqlException with a friendly message if over-dispatching.</summary>
        public static async Task DispatchAsync(int batchId, string personName, string personNumber, decimal qty, DateTime dispatchDate)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_DispatchBatch", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BatchId", batchId);
            cmd.Parameters.AddWithValue("@PersonName", personName);
            cmd.Parameters.AddWithValue("@PersonNumber", personNumber);
            cmd.Parameters.AddWithValue("@Qty", qty);
            cmd.Parameters.AddWithValue("@DispatchDate", dispatchDate.Date);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<BatchDispatchRecord>> GetDispatchesAsync(int batchId)
        {
            var list = new List<BatchDispatchRecord>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetBatchDispatches", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BatchId", batchId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new BatchDispatchRecord
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    PersonName = reader.GetString(reader.GetOrdinal("PersonName")),
                    PersonNumber = reader.GetString(reader.GetOrdinal("PersonNumber")),
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                    DispatchDate = reader.GetDateTime(reader.GetOrdinal("DispatchDate")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                });
            }

            return list;
        }

        public static async Task<List<LedgerEntry>> GetLedgerAsync()
        {
            var list = new List<LedgerEntry>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetFoodPacketLedger", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new LedgerEntry
                {
                    Type = reader.GetString(reader.GetOrdinal("Type")),
                    BatchCode = reader.GetString(reader.GetOrdinal("BatchCode")),
                    Item = reader.GetString(reader.GetOrdinal("Item")),
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                    Party = reader.GetString(reader.GetOrdinal("Party")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                });
            }

            return list;
        }
    }
}
