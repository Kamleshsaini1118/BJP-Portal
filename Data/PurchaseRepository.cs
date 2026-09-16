using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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

        public static async Task AddItemAsync(int purchaseId, string? category, string productName, decimal qty, decimal price, decimal gstPercent)
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
            await cmd.ExecuteNonQueryAsync();
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
