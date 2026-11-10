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
    public static class PurchaseRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<Purchase>> GetPurchasesAsync(
            string? vendorName, string? productName, DateTime? dateFrom, DateTime? dateTo)
        {
            var list = new List<Purchase>();

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

            return list;
        }

        public static async Task<List<PurchaseOrderItemRow>> GetItemsAsync(int purchaseId)
        {
            var list = new List<PurchaseOrderItemRow>();

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
            var list = new List<PendingChallan>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetPendingChallansByVendor", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@VendorName", vendorName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new PendingChallan
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ChallanNumber = reader.IsDBNull(reader.GetOrdinal("ChallanNumber")) ? "" : reader.GetString(reader.GetOrdinal("ChallanNumber")),
                    ChallanDate = reader.IsDBNull(reader.GetOrdinal("ChallanDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ChallanDate")),
                    PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                    ItemsList = reader.IsDBNull(reader.GetOrdinal("ItemsList")) ? "" : reader.GetString(reader.GetOrdinal("ItemsList")),
                    Total = reader.GetDecimal(reader.GetOrdinal("Total")),
                });
            }

            return list;
        }

        /// <summary>Links one purchase to a combined invoice (call once per selected challan).</summary>
        public static async Task LinkChallanToInvoiceAsync(int id, string invoiceNumber, DateTime? invoiceDate, int invoiceGroupId, string? appendRemark)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_LinkChallanToInvoice", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
            cmd.Parameters.AddWithValue("@InvoiceDate", (object?)invoiceDate?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@InvoiceGroupId", invoiceGroupId);
            cmd.Parameters.AddWithValue("@AppendRemark", (object?)appendRemark ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
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
            var list = serialNumbers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0) return null;

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            foreach (var serial in list)
            {
                using var cmd = new SqlCommand(@"
                    SELECT TOP 1 SerialNumber 
                    FROM dbo.PurchaseItems 
                    WHERE SerialNumber = @Serial 
                      AND (@PurchaseId IS NULL OR PurchaseId <> @PurchaseId);", conn);

                cmd.Parameters.AddWithValue("@Serial", serial);
                cmd.Parameters.AddWithValue("@PurchaseId", (object?)currentPurchaseId ?? DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    return serial;
                }
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
            var list = new List<PurchaseItemReportRow>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("dbo.sp_GetPurchaseItemsReport", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProductName", (object?)productName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate?.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate?.Date ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(ReadPurchaseItemReportRow(reader));
                }
            }
            catch (SqlException ex) when (ex.Number == 2812) // Stored procedure not found fallback
            {
                var sql = @"
                    SELECT 
                        pi.Id,
                        pi.PurchaseId,
                        pi.Category,
                        pi.ProductName,
                        pi.Qty,
                        pi.Price,
                        pi.GstPercent,
                        pi.SerialNumber,
                        p.PurchaseDate,
                        p.VendorName,
                        p.PoReference,
                        p.InvoiceNumber,
                        p.ChallanNumber,
                        p.PurchaseType,
                        p.PaymentMode
                    FROM dbo.PurchaseItems pi
                    INNER JOIN dbo.Purchases p ON pi.PurchaseId = p.Id
                    WHERE (@Search IS NULL 
                           OR pi.ProductName LIKE '%' + @Search + '%' 
                           OR pi.Category LIKE '%' + @Search + '%' 
                           OR pi.SerialNumber LIKE '%' + @Search + '%')
                      AND (@ProductName IS NULL OR pi.ProductName = @ProductName)
                      AND (@FromDate IS NULL OR p.PurchaseDate >= @FromDate)
                      AND (@ToDate IS NULL OR p.PurchaseDate <= @ToDate)
                    ORDER BY p.PurchaseDate DESC, pi.Id ASC;";

                using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text };
                cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProductName", (object?)productName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FromDate", (object?)fromDate?.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ToDate", (object?)toDate?.Date ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(ReadPurchaseItemReportRow(reader));
                }
            }

            return list;
        }

        private static PurchaseItemReportRow ReadPurchaseItemReportRow(SqlDataReader reader)
        {
            int idCol = -1;
            try { idCol = reader.GetOrdinal("ItemId"); } catch { }
            if (idCol < 0)
            {
                try { idCol = reader.GetOrdinal("Id"); } catch { }
            }

            int serialCol = -1;
            try { serialCol = reader.GetOrdinal("SerialNumber"); } catch { }

            int vendorCol = -1;
            try { vendorCol = reader.GetOrdinal("VendorName"); } catch { }

            int poCol = -1;
            try { poCol = reader.GetOrdinal("PoReference"); } catch { }

            int payCol = -1;
            try { payCol = reader.GetOrdinal("PaymentMode"); } catch { }

            int catCol = -1;
            try { catCol = reader.GetOrdinal("Category"); } catch { }

            return new PurchaseItemReportRow
            {
                ItemId = idCol >= 0 ? reader.GetInt32(idCol) : 0,
                PurchaseId = reader.GetInt32(reader.GetOrdinal("PurchaseId")),
                PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                VendorName = (vendorCol >= 0 && !reader.IsDBNull(vendorCol)) ? reader.GetString(vendorCol) : string.Empty,
                PoReference = (poCol >= 0 && !reader.IsDBNull(poCol)) ? reader.GetString(poCol) : null,
                PaymentMode = (payCol >= 0 && !reader.IsDBNull(payCol)) ? reader.GetString(payCol) : null,
                Category = (catCol >= 0 && !reader.IsDBNull(catCol)) ? reader.GetString(catCol) : null,
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                SerialNumber = (serialCol >= 0 && !reader.IsDBNull(serialCol)) ? reader.GetString(serialCol) : null,
                Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                GstPercent = reader.GetDecimal(reader.GetOrdinal("GstPercent")),
            };
        }
    }
}

















// using Microsoft.Data.SqlClient;
// using StockPortalApp.Models;
// using System;
// using System.Collections.Generic;
// using System.Configuration;
// using System.Data;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace StockPortalApp.Data
// {
//     public static class PurchaseRepository
//     {
//         private static string ConnectionString =>
//             ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

//         public static async Task<List<Purchase>> GetPurchasesAsync(
//             string? vendorName, string? productName, DateTime? dateFrom, DateTime? dateTo)
//         {
//             var list = new List<Purchase>();

//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_GetPurchases", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@VendorName", (object?)vendorName ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@ProductName", (object?)productName ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);

//             using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 list.Add(new Purchase
//                 {
//                     Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                     PurchaseType = reader.GetString(reader.GetOrdinal("PurchaseType")),
//                     PoReference = reader.IsDBNull(reader.GetOrdinal("PoReference")) ? null : reader.GetString(reader.GetOrdinal("PoReference")),
//                     InvoiceNumber = reader.IsDBNull(reader.GetOrdinal("InvoiceNumber")) ? null : reader.GetString(reader.GetOrdinal("InvoiceNumber")),
//                     VendorName = reader.GetString(reader.GetOrdinal("VendorName")),
//                     PaymentMode = reader.IsDBNull(reader.GetOrdinal("PaymentMode")) ? null : reader.GetString(reader.GetOrdinal("PaymentMode")),
//                     PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
//                     DistributionId = reader.IsDBNull(reader.GetOrdinal("DistributionId")) ? null : reader.GetInt32(reader.GetOrdinal("DistributionId")),
//                     ReceiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader.GetString(reader.GetOrdinal("ReceiverName")),
//                     ReceiverNumber = reader.IsDBNull(reader.GetOrdinal("ReceiverNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiverNumber")),
//                     Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? null : reader.GetString(reader.GetOrdinal("Remarks")),
//                     Total = reader.GetDecimal(reader.GetOrdinal("Total")),
//                 });
//             }

//             return list;
//         }

//         public static async Task<List<PurchaseOrderItemRow>> GetItemsAsync(int purchaseId)
//         {
//             var list = new List<PurchaseOrderItemRow>();

//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_GetPurchaseItems", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);

//             using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 list.Add(new PurchaseOrderItemRow(new List<Product>())
//                 {
//                     Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
//                     ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
//                     Qty = reader.GetDecimal(reader.GetOrdinal("Qty")).ToString(System.Globalization.CultureInfo.InvariantCulture),
//                     Price = reader.GetDecimal(reader.GetOrdinal("Price")).ToString(System.Globalization.CultureInfo.InvariantCulture),
//                     GstPercent = reader.GetDecimal(reader.GetOrdinal("GstPercent")),
//                 });
//             }

//             return list;
//         }

//         /// <summary>Returns the linked distribution's header fields plus its items. Null header fields if there's no linked distribution.</summary>
//         public static async Task<(string? ShipmentId, string? Destination, DateTime? Date, string? ReceiverName, string? ReceiverNumber, string? Position, string? Remark, List<(string? Category, string ProductName, decimal Qty)> Items)>
//             GetDistributionWithItemsAsync(int distributionId)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_GetDistributionWithItems", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@DistributionId", distributionId);

//             using var reader = await cmd.ExecuteReaderAsync();

//             string? shipmentId = null, destination = null, receiverName = null, receiverNumber = null, position = null, remark = null;
//             DateTime? date = null;

//             if (await reader.ReadAsync())
//             {
//                 shipmentId = reader.GetString(reader.GetOrdinal("ShipmentId"));
//                 destination = reader.GetString(reader.GetOrdinal("Destination"));
//                 date = reader.GetDateTime(reader.GetOrdinal("DistributionDate"));
//                 receiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader.GetString(reader.GetOrdinal("ReceiverName"));
//                 receiverNumber = reader.IsDBNull(reader.GetOrdinal("ReceiverNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiverNumber"));
//                 position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString(reader.GetOrdinal("Position"));
//                 remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark"));
//             }

//             var items = new List<(string? Category, string ProductName, decimal Qty)>();
//             await reader.NextResultAsync();
//             while (await reader.ReadAsync())
//             {
//                 items.Add((
//                     reader.IsDBNull(reader.GetOrdinal("Category")) ? null : reader.GetString(reader.GetOrdinal("Category")),
//                     reader.GetString(reader.GetOrdinal("ProductName")),
//                     reader.GetDecimal(reader.GetOrdinal("Qty"))
//                 ));
//             }

//             return (shipmentId, destination, date, receiverName, receiverNumber, position, remark, items);
//         }

//         /// <summary>Inserts (id == null) or updates a purchase header. Returns the Id.</summary>
//         public static async Task<int> SaveHeaderAsync(
//             int? id, string purchaseType, string? poReference, string? invoiceNumber, string vendorName,
//             string? paymentMode, DateTime purchaseDate, string? receiverName, string? receiverNumber,
//             string? remarks, int? distributionId)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_SavePurchaseHeader", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@Id", (object?)id ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@PurchaseType", purchaseType);
//             cmd.Parameters.AddWithValue("@PoReference", (object?)poReference ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@InvoiceNumber", (object?)invoiceNumber ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@VendorName", vendorName);
//             cmd.Parameters.AddWithValue("@PaymentMode", (object?)paymentMode ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@PurchaseDate", purchaseDate.Date);
//             cmd.Parameters.AddWithValue("@ReceiverName", (object?)receiverName ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@ReceiverNumber", (object?)receiverNumber ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@Remarks", (object?)remarks ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@DistributionId", (object?)distributionId ?? DBNull.Value);

//             using var reader = await cmd.ExecuteReaderAsync();
//             await reader.ReadAsync();
//             return reader.GetInt32(reader.GetOrdinal("Id"));
//         }

//         public static async Task ClearItemsAsync(int purchaseId)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_ClearPurchaseItems", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         public static async Task AddItemAsync(int purchaseId, string? category, string productName, decimal qty, decimal price, decimal gstPercent)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_AddPurchaseItem", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
//             cmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@ProductName", productName);
//             cmd.Parameters.AddWithValue("@Qty", qty);
//             cmd.Parameters.AddWithValue("@Price", price);
//             cmd.Parameters.AddWithValue("@GstPercent", gstPercent);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         /// <summary>Creates a new distribution (auto-generates its EXP-#### ShipmentId). Returns (Id, ShipmentId).</summary>
//         public static async Task<(int Id, string ShipmentId)> AddDistributionAsync(
//             string destination, DateTime distributionDate, string? receiverName, string? receiverNumber, string? position, string? remark)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_AddDistribution", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@Destination", destination);
//             cmd.Parameters.AddWithValue("@DistributionDate", distributionDate.Date);
//             cmd.Parameters.AddWithValue("@ReceiverName", (object?)receiverName ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@ReceiverNumber", (object?)receiverNumber ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@Position", (object?)position ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);

//             using var reader = await cmd.ExecuteReaderAsync();
//             await reader.ReadAsync();
//             return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("ShipmentId")));
//         }

//         public static async Task AddDistributionItemAsync(int distributionId, string? category, string productName, decimal qty)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_AddDistributionItem", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@DistributionId", distributionId);
//             cmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@ProductName", productName);
//             cmd.Parameters.AddWithValue("@Qty", qty);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         public static async Task UpdateDistributionHeaderAsync(
//             int id, string destination, DateTime distributionDate, string? receiverName, string? receiverNumber, string? position, string? remark)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_UpdateDistributionHeader", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@Id", id);
//             cmd.Parameters.AddWithValue("@Destination", destination);
//             cmd.Parameters.AddWithValue("@DistributionDate", distributionDate.Date);
//             cmd.Parameters.AddWithValue("@ReceiverName", (object?)receiverName ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@ReceiverNumber", (object?)receiverNumber ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@Position", (object?)position ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         public static async Task ClearDistributionItemsAsync(int distributionId)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_ClearDistributionItems", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@DistributionId", distributionId);
//             await cmd.ExecuteNonQueryAsync();
//         }
//         public static async Task<List<InvoiceFileEntry>> GetInvoiceFilesAsync(int purchaseId)
//         {
//             var list = new List<InvoiceFileEntry>();

//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_GetPurchaseInvoiceFiles", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);

//             using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 list.Add(new InvoiceFileEntry
//                 {
//                     Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                     FileName = reader.GetString(reader.GetOrdinal("FileName")),
//                 });
//             }

//             return list;
//         }

//         public static async Task AddInvoiceFileAsync(int purchaseId, string fileName, byte[] data)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_AddPurchaseInvoiceFile", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
//             cmd.Parameters.AddWithValue("@FileName", fileName);
//             cmd.Parameters.AddWithValue("@FileData", data);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         public static async Task DeleteInvoiceFileAsync(int id)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_DeletePurchaseInvoiceFile", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@Id", id);
//             await cmd.ExecuteNonQueryAsync();
//         }
//     }
// }
