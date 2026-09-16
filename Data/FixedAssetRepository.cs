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
    public static class FixedAssetRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<FixedAssetSummary>> GetSummaryAsync()
        {
            var list = new List<FixedAssetSummary>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetFixedAssetsSummary", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new FixedAssetSummary
                {
                    Category = reader.GetString(reader.GetOrdinal("Category")),
                    Item = reader.GetString(reader.GetOrdinal("Item")),
                    Code = reader.GetString(reader.GetOrdinal("Code")),
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                    Value = reader.GetDecimal(reader.GetOrdinal("Value")),
                });
            }

            return list;
        }

        public static async Task<List<AssetDistributionDetail>> GetDistributionDetailAsync(string itemName)
        {
            var list = new List<AssetDistributionDetail>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetAssetDistributionDetail", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ItemName", itemName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new AssetDistributionDetail
                {
                    Destination = reader.GetString(reader.GetOrdinal("Destination")),
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                    ReceiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader.GetString(reader.GetOrdinal("ReceiverName")),
                    ReceiverNumber = reader.IsDBNull(reader.GetOrdinal("ReceiverNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiverNumber")),
                    Position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString(reader.GetOrdinal("Position")),
                    DistributionDate = reader.GetDateTime(reader.GetOrdinal("DistributionDate")),
                });
            }

            return list;
        }
    }
}
