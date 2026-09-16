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
    public static class PurchaseOrderRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<PurchaseOrder>> GetOrdersAsync(
            string? vendorName, string? productName, DateTime? dateFrom, DateTime? dateTo)
        {
            var list = new List<PurchaseOrder>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetPurchaseOrders", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@VendorName", (object?)vendorName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductName", (object?)productName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PurchaseOrder
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    PoReference = reader.GetString(reader.GetOrdinal("PoReference")),
                    VendorName = reader.GetString(reader.GetOrdinal("VendorName")),
                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                    Delivery = reader.IsDBNull(reader.GetOrdinal("Delivery")) ? null : reader.GetDateTime(reader.GetOrdinal("Delivery")),
                    ItemsList = reader.IsDBNull(reader.GetOrdinal("ItemsList")) ? "" : reader.GetString(reader.GetOrdinal("ItemsList")),
                    TotalQty = reader.GetDecimal(reader.GetOrdinal("TotalQty")),
                });
            }

            return list;
        }

        public static async Task<List<PurchaseOrderItemRow>> GetItemsAsync(int purchaseOrderId)
        {
            var list = new List<PurchaseOrderItemRow>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetPurchaseOrderItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PurchaseOrderItemRow(new List<Product>())
                {
                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Price = reader.GetDecimal(reader.GetOrdinal("Price")).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    GstPercent = reader.GetDecimal(reader.GetOrdinal("GstPercent")),
                });
            }

            return list;
        }

        /// <summary>Inserts (id == null) or updates a PO header. Returns (Id, PoReference).</summary>
        public static async Task<(int Id, string PoReference)> SaveHeaderAsync(
            int? id, string vendorName, DateTime? delivery, string? remarks)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SavePurchaseOrderHeader", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", (object?)id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorName", vendorName);
            cmd.Parameters.AddWithValue("@Delivery", (object?)delivery?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("PoReference")));
        }

        public static async Task ClearItemsAsync(int purchaseOrderId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ClearPurchaseOrderItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task AddItemAsync(
            int purchaseOrderId, string? category, string productName, decimal qty, decimal price, decimal gstPercent)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddPurchaseOrderItem", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);
            cmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductName", productName);
            cmd.Parameters.AddWithValue("@Qty", qty);
            cmd.Parameters.AddWithValue("@Price", price);
            cmd.Parameters.AddWithValue("@GstPercent", gstPercent);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
