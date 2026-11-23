using Microsoft.Data.SqlClient;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.PurchaseOrders;
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
            // API Call
            var qParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(vendorName)) qParams.Add($"vendor={Uri.EscapeDataString(vendorName)}");
            if (!string.IsNullOrWhiteSpace(productName)) qParams.Add($"product={Uri.EscapeDataString(productName)}");
            if (dateFrom.HasValue) qParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue) qParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
            var url = "purchase-orders" + (qParams.Count > 0 ? "?" + string.Join("&", qParams) : "");

            var response = await ApiClient.GetAsync<List<PurchaseOrder>>(url);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
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
            */
            return new List<PurchaseOrder>();
        }

        public static async Task<List<PurchaseOrderItemRow>> GetItemsAsync(int purchaseOrderId)
        {
            // API Call
            var response = await ApiClient.GetAsync<List<PurchaseOrderItemDisplayDto>>($"purchase-orders/{purchaseOrderId}/items");
            if (response.Success && response.Data != null)
            {
                return response.Data.Select(d => new PurchaseOrderItemRow(new List<Product>())
                {
                    Category = d.Category,
                    ProductName = d.ProductName,
                    Qty = d.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Price = d.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    GstPercent = d.GstPercent
                }).ToList();
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
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
            */
            return new List<PurchaseOrderItemRow>();
        }

        public static async Task<(int Id, string PoReference)> SaveOrderAsync(
            int? id, string vendorName, DateTime? delivery, string? remarks, List<PurchaseOrderItemRow> items)
        {
            var dto = new
            {
                Id = id,
                VendorName = vendorName,
                Delivery = delivery,
                Remarks = remarks,
                Items = items.Select(r => new
                {
                    Category = r.Category,
                    ProductName = r.ProductName,
                    Qty = decimal.TryParse(r.Qty, out var q) ? q : 0,
                    Price = decimal.TryParse(r.Price, out var p) ? p : 0,
                    GstPercent = r.GstPercent
                }).ToList()
            };

            var response = await ApiClient.PostAsync<Dictionary<string, object>>("purchase-orders", dto);
            if (response.Success && response.Data != null)
            {
                int poId = 0;
                string poRef = string.Empty;
                if (response.Data.TryGetValue("id", out var idVal) || response.Data.TryGetValue("Id", out idVal))
                {
                    int.TryParse(idVal?.ToString(), out poId);
                }
                if (response.Data.TryGetValue("poReference", out var refVal) || response.Data.TryGetValue("PoReference", out refVal))
                {
                    poRef = refVal?.ToString() ?? string.Empty;
                }
                return (poId, poRef);
            }

            throw new InvalidOperationException(response.Message ?? "Failed to save purchase order.");
        }

        /*
        // --- DIRECT SQL CODE (COMMENTED) ---
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

        public static async Task AddItemAsync(int purchaseOrderId, string? category, string productName, decimal qty, decimal price, decimal gstPercent)
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
        */
    }
}
