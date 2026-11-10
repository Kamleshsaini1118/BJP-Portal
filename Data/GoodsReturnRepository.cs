using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StockPortalApp.Models;

namespace StockPortalApp.Data
{
    public static class GoodsReturnRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<GoodsReturn>> GetGoodsReturnsAsync(
            string? vendorName = null,
            string? category = null,
            string? productName = null,
            string? reason = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? searchQuery = null)
        {
            var list = new List<GoodsReturn>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetGoodsReturns", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@VendorName", string.IsNullOrWhiteSpace(vendorName) ? DBNull.Value : vendorName);
            cmd.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(category) ? DBNull.Value : category);
            cmd.Parameters.AddWithValue("@ProductName", string.IsNullOrWhiteSpace(productName) ? DBNull.Value : productName);
            cmd.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason);
            cmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SearchQuery", string.IsNullOrWhiteSpace(searchQuery) ? DBNull.Value : searchQuery.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new GoodsReturn
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ReturnNo = reader.GetString(reader.GetOrdinal("ReturnNo")),
                    ReturnDate = reader.GetDateTime(reader.GetOrdinal("ReturnDate")),
                    VendorName = reader.GetString(reader.GetOrdinal("VendorName")),
                    PurchaseId = reader.GetInt32(reader.GetOrdinal("PurchaseId")),
                    AgainstPurchaseRef = reader.IsDBNull(reader.GetOrdinal("AgainstPurchaseRef")) ? "" : reader.GetString(reader.GetOrdinal("AgainstPurchaseRef")),
                    PurchaseDate = reader.IsDBNull(reader.GetOrdinal("PurchaseDate")) ? null : reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                    Reason = reader.GetString(reader.GetOrdinal("Reason")),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                    TotalQty = reader.GetDecimal(reader.GetOrdinal("TotalQty")),
                    ItemsSummary = reader.IsDBNull(reader.GetOrdinal("ItemsSummary")) ? "" : reader.GetString(reader.GetOrdinal("ItemsSummary")),
                    CategoriesList = reader.IsDBNull(reader.GetOrdinal("CategoriesList")) ? "" : reader.GetString(reader.GetOrdinal("CategoriesList")),
                    ProductsList = reader.IsDBNull(reader.GetOrdinal("ProductsList")) ? "" : reader.GetString(reader.GetOrdinal("ProductsList")),
                    HandedOverTo = reader.IsDBNull(reader.GetOrdinal("HandedOverTo")) ? null : reader.GetString(reader.GetOrdinal("HandedOverTo")),
                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("ContactNumber")) ? null : reader.GetString(reader.GetOrdinal("ContactNumber")),
                    Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? null : reader.GetString(reader.GetOrdinal("Remarks")),
                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                });
            }

            return list;
        }

        public static async Task<(GoodsReturn? Header, List<GoodsReturnItem> Items)> GetGoodsReturnDetailsAsync(int goodsReturnId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetGoodsReturnDetails", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@GoodsReturnId", goodsReturnId);

            using var reader = await cmd.ExecuteReaderAsync();
            GoodsReturn? header = null;

            if (await reader.ReadAsync())
            {
                header = new GoodsReturn
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ReturnNo = reader.GetString(reader.GetOrdinal("ReturnNo")),
                    ReturnDate = reader.GetDateTime(reader.GetOrdinal("ReturnDate")),
                    VendorName = reader.GetString(reader.GetOrdinal("VendorName")),
                    PurchaseId = reader.GetInt32(reader.GetOrdinal("PurchaseId")),
                    AgainstPurchaseRef = reader.IsDBNull(reader.GetOrdinal("AgainstPurchaseRef")) ? "" : reader.GetString(reader.GetOrdinal("AgainstPurchaseRef")),
                    PurchaseDate = reader.IsDBNull(reader.GetOrdinal("PurchaseDate")) ? null : reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                    PoReference = reader.IsDBNull(reader.GetOrdinal("PoReference")) ? null : reader.GetString(reader.GetOrdinal("PoReference")),
                    Reason = reader.GetString(reader.GetOrdinal("Reason")),
                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                    HandedOverTo = reader.IsDBNull(reader.GetOrdinal("HandedOverTo")) ? null : reader.GetString(reader.GetOrdinal("HandedOverTo")),
                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("ContactNumber")) ? null : reader.GetString(reader.GetOrdinal("ContactNumber")),
                    Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? null : reader.GetString(reader.GetOrdinal("Remarks")),
                    VendorGst = reader.IsDBNull(reader.GetOrdinal("VendorGst")) ? null : reader.GetString(reader.GetOrdinal("VendorGst")),
                    VendorAddress = reader.IsDBNull(reader.GetOrdinal("VendorAddress")) ? null : reader.GetString(reader.GetOrdinal("VendorAddress")),
                    VendorContactPerson = reader.IsDBNull(reader.GetOrdinal("VendorContactPerson")) ? null : reader.GetString(reader.GetOrdinal("VendorContactPerson")),
                    VendorContactNumber = reader.IsDBNull(reader.GetOrdinal("VendorContactNumber")) ? null : reader.GetString(reader.GetOrdinal("VendorContactNumber")),
                };
            }

            var items = new List<GoodsReturnItem>();
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new GoodsReturnItem
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        GoodsReturnId = reader.GetInt32(reader.GetOrdinal("GoodsReturnId")),
                        PurchaseItemId = reader.IsDBNull(reader.GetOrdinal("PurchaseItemId")) ? null : reader.GetInt32(reader.GetOrdinal("PurchaseItemId")),
                        Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                        ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                        SerialNumber = reader.IsDBNull(reader.GetOrdinal("SerialNumber")) ? null : reader.GetString(reader.GetOrdinal("SerialNumber")),
                        ReturnQty = reader.GetDecimal(reader.GetOrdinal("ReturnQty")),
                        Rate = reader.GetDecimal(reader.GetOrdinal("Rate")),
                        GstPercent = reader.GetDecimal(reader.GetOrdinal("GstPercent")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                    });
                }
            }

            return (header, items);
        }

        public static async Task<List<ReturnablePurchaseOption>> GetVendorPurchasesForReturnAsync(string vendorName)
        {
            var list = new List<ReturnablePurchaseOption>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetVendorPurchasesForReturn", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@VendorName", vendorName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReturnablePurchaseOption
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    PurchaseType = reader.IsDBNull(reader.GetOrdinal("PurchaseType")) ? "" : reader.GetString(reader.GetOrdinal("PurchaseType")),
                    ReceiptBasis = reader.IsDBNull(reader.GetOrdinal("ReceiptBasis")) ? "Invoice" : reader.GetString(reader.GetOrdinal("ReceiptBasis")),
                    InvoiceNumber = reader.IsDBNull(reader.GetOrdinal("InvoiceNumber")) ? null : reader.GetString(reader.GetOrdinal("InvoiceNumber")),
                    ChallanNumber = reader.IsDBNull(reader.GetOrdinal("ChallanNumber")) ? null : reader.GetString(reader.GetOrdinal("ChallanNumber")),
                    PoReference = reader.IsDBNull(reader.GetOrdinal("PoReference")) ? null : reader.GetString(reader.GetOrdinal("PoReference")),
                    PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                });
            }

            return list;
        }

        public static async Task<List<ReturnablePurchaseItemRow>> GetPurchaseItemsForReturnAsync(int purchaseId)
        {
            var list = new List<ReturnablePurchaseItemRow>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetPurchaseItemsForReturn", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ReturnablePurchaseItemRow
                {
                    PurchaseItemId = reader.GetInt32(reader.GetOrdinal("PurchaseItemId")),
                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    SerialNumber = reader.IsDBNull(reader.GetOrdinal("SerialNumber")) ? null : reader.GetString(reader.GetOrdinal("SerialNumber")),
                    PurchasedQty = reader.GetDecimal(reader.GetOrdinal("PurchasedQty")),
                    ReturnedEarlierQty = reader.GetDecimal(reader.GetOrdinal("ReturnedEarlierQty")),
                    Rate = reader.GetDecimal(reader.GetOrdinal("Rate")),
                    GstPercent = reader.GetDecimal(reader.GetOrdinal("GstPercent")),
                });
            }

            return list;
        }

        public static async Task<(int Id, string ReturnNo)> SaveGoodsReturnAsync(
            DateTime returnDate,
            string vendorName,
            int purchaseId,
            string? purchaseRef,
            string reason,
            decimal totalAmount,
            string? handedOverTo,
            string? contactNumber,
            string? remarks,
            IEnumerable<ReturnablePurchaseItemRow> items)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();
            try
            {
                int returnId;
                string returnNo;

                using (var headerCmd = new SqlCommand("dbo.sp_AddGoodsReturnHeader", conn, tx)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    headerCmd.Parameters.AddWithValue("@ReturnDate", returnDate.Date);
                    headerCmd.Parameters.AddWithValue("@VendorName", vendorName);
                    headerCmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
                    headerCmd.Parameters.AddWithValue("@PurchaseRef", string.IsNullOrWhiteSpace(purchaseRef) ? DBNull.Value : purchaseRef);
                    headerCmd.Parameters.AddWithValue("@Reason", reason);
                    headerCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    headerCmd.Parameters.AddWithValue("@HandedOverTo", string.IsNullOrWhiteSpace(handedOverTo) ? DBNull.Value : handedOverTo.Trim());
                    headerCmd.Parameters.AddWithValue("@ContactNumber", string.IsNullOrWhiteSpace(contactNumber) ? DBNull.Value : contactNumber.Trim());
                    headerCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : remarks.Trim());

                    using var reader = await headerCmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    returnId = reader.GetInt32(reader.GetOrdinal("Id"));
                    returnNo = reader.GetString(reader.GetOrdinal("ReturnNo"));
                }

                foreach (var item in items)
                {
                    if (item.ReturnQty <= 0) continue;

                    using var itemCmd = new SqlCommand("dbo.sp_AddGoodsReturnItem", conn, tx)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    itemCmd.Parameters.AddWithValue("@GoodsReturnId", returnId);
                    itemCmd.Parameters.AddWithValue("@PurchaseItemId", item.PurchaseItemId);
                    itemCmd.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(item.Category) ? DBNull.Value : item.Category);
                    itemCmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                    itemCmd.Parameters.AddWithValue("@SerialNumber", string.IsNullOrWhiteSpace(item.SerialNumber) ? DBNull.Value : item.SerialNumber);
                    itemCmd.Parameters.AddWithValue("@ReturnQty", item.ReturnQty);
                    itemCmd.Parameters.AddWithValue("@Rate", item.Rate);
                    itemCmd.Parameters.AddWithValue("@GstPercent", item.GstPercent);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (returnId, returnNo);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
