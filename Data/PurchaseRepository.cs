using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.Purchases;

namespace StockPortalApp.Data
{
    public static class PurchaseRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<Purchase>> GetPurchasesAsync(
            string? vendorName, string? productName, DateTime? dateFrom, DateTime? dateTo)
        {
            // API Call
            try
            {
                var qParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(vendorName)) qParams.Add($"vendor={Uri.EscapeDataString(vendorName)}");
                if (dateFrom.HasValue) qParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
                if (dateTo.HasValue) qParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
                var url = "purchases" + (qParams.Count > 0 ? "?" + string.Join("&", qParams) : "");

                var response = await ApiClient.GetAsync<List<PurchaseDto>>(url);
                if (response.Success && response.Data != null && response.Data.Count > 0)
                {
                    return response.Data.Select(d => new Purchase
                    {
                        Id = d.Id,
                        PurchaseType = d.PurchaseType ?? "Direct",
                        PoReference = d.PoReference,
                        InvoiceNumber = d.InvoiceNumber,
                        VendorName = d.VendorName ?? "",
                        PaymentMode = d.PaymentMode,
                        PurchaseDate = d.PurchaseDate,
                        DistributionId = d.DistributionId,
                        ReceiverName = d.ReceiverName,
                        ReceiverNumber = d.ReceiverNumber,
                        Remarks = d.Remarks,
                        ReceiptBasis = d.ReceiptBasis ?? "Invoice",
                        ChallanNumber = d.ChallanNumber,
                        ChallanDate = d.ChallanDate,
                        InvoiceDate = d.InvoiceDate,
                        InvoiceStatus = d.InvoiceStatus ?? "Invoiced",
                        InvoiceGroupId = d.InvoiceGroupId,
                        Total = d.Total,
                        SerialNumbers = d.SerialNumbers,
                        LinkedChallansDisplay = d.LinkedChallansDisplay ?? ""
                    }).ToList();
                }
            }
            catch { }

            return await GetPurchasesDirectSqlAsync(vendorName, productName, dateFrom, dateTo);
        }

        private static async Task<List<Purchase>> GetPurchasesDirectSqlAsync(
            string? vendorName, string? productName, DateTime? dateFrom, DateTime? dateTo)
        {
            var list = new List<Purchase>();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.sp_GetPurchases", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@VendorName", (object?)vendorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProductName", (object?)productName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                int serialCol = -1;
                try { serialCol = reader.GetOrdinal("SerialNumbers"); } catch { }
                if (serialCol == -1)
                {
                    try { serialCol = reader.GetOrdinal("SerialNumber"); } catch { }
                }

                while (await reader.ReadAsync())
                {
                    list.Add(new Purchase
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        PurchaseType = reader.GetString(reader.GetOrdinal("PurchaseType")),
                        PoReference = reader.IsDBNull(reader.GetOrdinal("PoReference")) ? null : reader.GetString(reader.GetOrdinal("PoReference")),
                        InvoiceNumber = reader.IsDBNull(reader.GetOrdinal("InvoiceNumber")) ? null : reader.GetString(reader.GetOrdinal("InvoiceNumber")),
                        VendorName = reader.GetString(reader.GetOrdinal("VendorName")),
                        PaymentMode = reader.IsDBNull(reader.GetOrdinal("PaymentMode")) ? null : reader.GetString(reader.GetOrdinal("PaymentMode")),
                        PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                        DistributionId = reader.IsDBNull(reader.GetOrdinal("DistributionId")) ? null : reader.GetInt32(reader.GetOrdinal("DistributionId")),
                        ReceiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader.GetString(reader.GetOrdinal("ReceiverName")),
                        ReceiverNumber = reader.IsDBNull(reader.GetOrdinal("ReceiverNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiverNumber")),
                        Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? null : reader.GetString(reader.GetOrdinal("Remarks")),
                        ReceiptBasis = reader.GetString(reader.GetOrdinal("ReceiptBasis")),
                        ChallanNumber = reader.IsDBNull(reader.GetOrdinal("ChallanNumber")) ? null : reader.GetString(reader.GetOrdinal("ChallanNumber")),
                        ChallanDate = reader.IsDBNull(reader.GetOrdinal("ChallanDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ChallanDate")),
                        InvoiceDate = reader.IsDBNull(reader.GetOrdinal("InvoiceDate")) ? null : reader.GetDateTime(reader.GetOrdinal("InvoiceDate")),
                        InvoiceStatus = reader.GetString(reader.GetOrdinal("InvoiceStatus")),
                        InvoiceGroupId = reader.IsDBNull(reader.GetOrdinal("InvoiceGroupId")) ? null : reader.GetInt32(reader.GetOrdinal("InvoiceGroupId")),
                        Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                        SerialNumbers = (serialCol >= 0 && !reader.IsDBNull(serialCol)) ? reader.GetString(serialCol) : null,
                    });
                }
            }
            catch { }

            return list;
        }

        public static async Task<List<PurchaseOrderItemRow>> GetItemsAsync(int purchaseId)
        {
            // API Call
            try
            {
                var response = await ApiClient.GetAsync<List<PurchaseItemDto>>($"purchases/{purchaseId}/items");
                if (response.Success && response.Data != null && response.Data.Count > 0)
                {
                    return response.Data.Select(d => new PurchaseOrderItemRow(new List<Product>())
                    {
                        Category = d.Category,
                        ProductName = d.ProductName,
                        Qty = d.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Price = d.Price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        GstPercent = d.GstPercent,
                        SerialNumber = d.SerialNumber
                    }).ToList();
                }
            }
            catch { }

            return await GetItemsDirectSqlAsync(purchaseId);
        }

        private static async Task<List<PurchaseOrderItemRow>> GetItemsDirectSqlAsync(int purchaseId)
        {
            var list = new List<PurchaseOrderItemRow>();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.sp_GetPurchaseItems", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);

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
                        SerialNumber = reader.IsDBNull(reader.GetOrdinal("SerialNumber")) ? null : reader.GetString(reader.GetOrdinal("SerialNumber")),
                    });
                }
            }
            catch { }

            return list;
        }

        /// <summary>Returns the linked distribution's header fields plus its items. Null header fields if there's no linked distribution.</summary>
        public static async Task<(string? ShipmentId, string? Destination, DateTime? Date, string? ReceiverName, string? ReceiverNumber, string? Position, string? Remark, List<(string? Category, string ProductName, decimal Qty)> Items)>
            GetDistributionWithItemsAsync(int distributionId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetDistributionWithItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DistributionId", distributionId);

            using var reader = await cmd.ExecuteReaderAsync();

            string? shipmentId = null, destination = null, receiverName = null, receiverNumber = null, position = null, remark = null;
            DateTime? date = null;

            if (await reader.ReadAsync())
            {
                shipmentId = reader.GetString(reader.GetOrdinal("ShipmentId"));
                destination = reader.GetString(reader.GetOrdinal("Destination"));
                date = reader.GetDateTime(reader.GetOrdinal("DistributionDate"));
                receiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader.GetString(reader.GetOrdinal("ReceiverName"));
                receiverNumber = reader.IsDBNull(reader.GetOrdinal("ReceiverNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiverNumber"));
                position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString(reader.GetOrdinal("Position"));
                remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark"));
            }

            var items = new List<(string? Category, string ProductName, decimal Qty)>();
            await reader.NextResultAsync();
            while (await reader.ReadAsync())
            {
                items.Add((
                    reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
                    reader.GetString(reader.GetOrdinal("ProductName")),
                    reader.GetDecimal(reader.GetOrdinal("Qty"))
                ));
            }

            return (shipmentId, destination, date, receiverName, receiverNumber, position, remark, items);
        }

        /// <summary>Inserts (id == null) or updates a purchase header. Returns the Id.</summary>
        public static async Task<int> SaveHeaderAsync(
            int? id, string purchaseType, string? poReference, string? invoiceNumber, string vendorName,
            string? paymentMode, DateTime purchaseDate, string? receiverName, string? receiverNumber,
            string? remarks, int? distributionId,
            string receiptBasis, string? challanNumber, DateTime? challanDate, DateTime? invoiceDate,
            string invoiceStatus, int? invoiceGroupId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SavePurchaseHeader", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", (object?)id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PurchaseType", purchaseType);
            cmd.Parameters.AddWithValue("@PoReference", (object?)poReference ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InvoiceNumber", (object?)invoiceNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorName", vendorName);
            cmd.Parameters.AddWithValue("@PaymentMode", (object?)paymentMode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PurchaseDate", purchaseDate.Date);
            cmd.Parameters.AddWithValue("@ReceiverName", (object?)receiverName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiverNumber", (object?)receiverNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DistributionId", (object?)distributionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiptBasis", receiptBasis);
            cmd.Parameters.AddWithValue("@ChallanNumber", (object?)challanNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ChallanDate", (object?)challanDate?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InvoiceDate", (object?)invoiceDate?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InvoiceStatus", invoiceStatus);
            cmd.Parameters.AddWithValue("@InvoiceGroupId", (object?)invoiceGroupId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32(reader.GetOrdinal("Id"));
        }

        /// <summary>Pending (not-yet-invoiced) challan purchases for one vendor, for the Combine Challans picker.</summary>
        public static async Task<List<PendingChallan>> GetPendingChallansByVendorAsync(string vendorName)
        {
            // API Call
            var response = await ApiClient.GetAsync<List<PendingChallan>>($"purchases/pending-challans?vendorName={Uri.EscapeDataString(vendorName)}");
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            return new List<PendingChallan>();
        }

        /// <summary>Links one purchase to a combined invoice (call once per selected challan).</summary>
        public static async Task LinkChallanToInvoiceAsync(int id, string invoiceNumber, DateTime? invoiceDate, int invoiceGroupId, string? appendRemark)
        {
            // API Call
            var dto = new LinkChallanRequestDto
            {
                Id = id,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = invoiceDate,
                InvoiceGroupId = invoiceGroupId,
                AppendRemark = appendRemark
            };
            await ApiClient.PostAsync<bool>("purchases/link-challan", dto);
        }

        public static async Task ClearItemsAsync(int purchaseId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ClearPurchaseItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task AddItemAsync(int purchaseId, string? category, string productName, decimal qty, decimal price, decimal gstPercent, string? serialNumber = null)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddPurchaseItem", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
            cmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductName", productName);
            cmd.Parameters.AddWithValue("@Qty", qty);
            cmd.Parameters.AddWithValue("@Price", price);
            cmd.Parameters.AddWithValue("@GstPercent", gstPercent);
            cmd.Parameters.AddWithValue("@SerialNumber", (object?)serialNumber ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Checks whether any of the given serial numbers already exist in the database (excluding a specific purchase if editing).
        /// Returns the first conflicting serial number, or null if all are unique.
        /// </summary>
        public static async Task<string?> CheckExistingSerialNumberAsync(IEnumerable<string> serialNumbers, int? currentPurchaseId = null)
        {
            // API Call
            var dto = new CheckSerialNumbersRequestDto
            {
                SerialNumbers = serialNumbers.ToList(),
                CurrentPurchaseId = currentPurchaseId
            };
            var response = await ApiClient.PostAsync<string?>("purchases/check-serials", dto);
            if (response.Success)
            {
                return response.Data;
            }

            return null;
        }

        /// <summary>
        /// Cleans up a partially created purchase (and its linked distribution if direct) if an error occurred during item insertion.
        /// </summary>
        public static async Task DeleteIncompletePurchaseAsync(int purchaseId, int? distributionId = null)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var cmd1 = new SqlCommand("DELETE FROM dbo.PurchaseInvoiceFiles WHERE PurchaseId = @PurchaseId", conn);
                cmd1.Parameters.AddWithValue("@PurchaseId", purchaseId);
                await cmd1.ExecuteNonQueryAsync();

                using var cmd2 = new SqlCommand("DELETE FROM dbo.PurchaseItems WHERE PurchaseId = @PurchaseId", conn);
                cmd2.Parameters.AddWithValue("@PurchaseId", purchaseId);
                await cmd2.ExecuteNonQueryAsync();

                using var cmd3 = new SqlCommand("DELETE FROM dbo.Purchases WHERE Id = @PurchaseId", conn);
                cmd3.Parameters.AddWithValue("@PurchaseId", purchaseId);
                await cmd3.ExecuteNonQueryAsync();

                if (distributionId.HasValue)
                {
                    using var cmdDistItems = new SqlCommand("DELETE FROM dbo.DistributionItems WHERE DistributionId = @DistId", conn);
                    cmdDistItems.Parameters.AddWithValue("@DistId", distributionId.Value);
                    await cmdDistItems.ExecuteNonQueryAsync();

                    using var cmdDist = new SqlCommand("DELETE FROM dbo.Distributions WHERE Id = @DistId", conn);
                    cmdDist.Parameters.AddWithValue("@DistId", distributionId.Value);
                    await cmdDist.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // Ignore cleanup errors so main exception is presented
            }
        }

        /// <summary>Creates a new distribution (auto-generates its EXP-#### ShipmentId). Returns (Id, ShipmentId).</summary>
        public static async Task<(int Id, string ShipmentId)> AddDistributionAsync(
            string destination, DateTime distributionDate, string? receiverName, string? receiverNumber, string? position, string? remark)
        {
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

        public static async Task AddDistributionItemAsync(int distributionId, string? category, string productName, decimal qty)
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

        public static async Task UpdateDistributionHeaderAsync(
            int id, string destination, DateTime distributionDate, string? receiverName, string? receiverNumber, string? position, string? remark)
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

        public static async Task ClearDistributionItemsAsync(int distributionId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ClearDistributionItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@DistributionId", distributionId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ---- Invoice files ----

        public static async Task<List<InvoiceFileEntry>> GetInvoiceFilesAsync(int purchaseId)
        {
            var list = new List<InvoiceFileEntry>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetPurchaseInvoiceFiles", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new InvoiceFileEntry
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FileName = reader.GetString(reader.GetOrdinal("FileName")),
                });
            }

            return list;
        }

        public static async Task AddInvoiceFileAsync(int purchaseId, string fileName, byte[] data)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddPurchaseInvoiceFile", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
            cmd.Parameters.AddWithValue("@FileName", fileName);
            cmd.Parameters.AddWithValue("@FileData", data);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task DeleteInvoiceFileAsync(int id)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_DeletePurchaseInvoiceFile", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<PurchaseItemReportRow>> GetPurchaseItemsReportAsync(
            string? search, string? productName, DateTime? fromDate, DateTime? toDate)
        {
            // API Call
            var qParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) qParams.Add($"vendor={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrWhiteSpace(productName)) qParams.Add($"category={Uri.EscapeDataString(productName)}");
            if (fromDate.HasValue) qParams.Add($"dateFrom={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue) qParams.Add($"dateTo={toDate.Value:yyyy-MM-dd}");
            var url = "reports/purchase-items" + (qParams.Count > 0 ? "?" + string.Join("&", qParams) : "");
            var response = await ApiClient.GetAsync<List<PurchaseItemReportRow>>(url);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            return new List<PurchaseItemReportRow>();
        }
    }
}