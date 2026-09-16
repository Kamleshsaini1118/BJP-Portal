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
    public static class DistributionRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<DistributionSummary>> GetDistributionsAsync()
        {
            var list = new List<DistributionSummary>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetDistributions", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new DistributionSummary
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ShipmentId = reader.GetString(reader.GetOrdinal("ShipmentId")),
                    Destination = reader.GetString(reader.GetOrdinal("Destination")),
                    DistributionDate = reader.GetDateTime(reader.GetOrdinal("DistributionDate")),
                    ReceiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader.GetString(reader.GetOrdinal("ReceiverName")),
                    ReceiverNumber = reader.IsDBNull(reader.GetOrdinal("ReceiverNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiverNumber")),
                    Position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString(reader.GetOrdinal("Position")),
                    Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                    ItemsList = reader.IsDBNull(reader.GetOrdinal("ItemsList")) ? "" : reader.GetString(reader.GetOrdinal("ItemsList")),
                    TotalQty = reader.GetDecimal(reader.GetOrdinal("TotalQty")),
                });
            }

            return list;
        }

        public static async Task<List<DistributionItemRow>> GetItemsAsync(int distributionId)
        {
            var list = new List<DistributionItemRow>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetDistributionWithItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DistributionId", distributionId);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync(); // header result set -- skip, caller already has the header
            await reader.NextResultAsync();

            while (await reader.ReadAsync())
            {
                var row = new DistributionItemRow(new List<Product>())
                {
                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                };
                row.ProductName = reader.GetString(reader.GetOrdinal("ProductName"));
                row.Qty = reader.GetDecimal(reader.GetOrdinal("Qty")).ToString(System.Globalization.CultureInfo.InvariantCulture);
                list.Add(row);
            }

            return list;
        }

        /// <summary>Inserts (id == null) or updates a distribution header. Returns (Id, ShipmentId).</summary>
        public static async Task<(int Id, string ShipmentId)> SaveHeaderAsync(
            int? id, string destination, DateTime distributionDate,
            string? receiverName, string? receiverNumber, string? position, string? remark)
        {
            if (id.HasValue)
            {
                await UpdateHeaderAsync(id.Value, destination, distributionDate, receiverName, receiverNumber, position, remark);
                return (id.Value, "");
            }

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddDistribution", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Destination", destination);
            cmd.Parameters.AddWithValue("@DistributionDate", distributionDate.Date);
            cmd.Parameters.AddWithValue("@ReceiverName", (object?)receiverName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiverNumber", (object?)receiverNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Position", (object?)position ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("ShipmentId")));
        }

        private static async Task UpdateHeaderAsync(
            int id, string destination, DateTime distributionDate,
            string? receiverName, string? receiverNumber, string? position, string? remark)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_UpdateDistributionHeader", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Destination", destination);
            cmd.Parameters.AddWithValue("@DistributionDate", distributionDate.Date);
            cmd.Parameters.AddWithValue("@ReceiverName", (object?)receiverName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiverNumber", (object?)receiverNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Position", (object?)position ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task ClearItemsAsync(int distributionId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ClearDistributionItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DistributionId", distributionId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task AddItemAsync(int distributionId, string? category, string productName, decimal qty)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddDistributionItem", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DistributionId", distributionId);
            cmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductName", productName);
            cmd.Parameters.AddWithValue("@Qty", qty);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
