using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StockPortalApp.Models;

namespace StockPortalApp.Data
{
    public static class IssueRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        /// <summary>List: one row per item issued. Pass status = "Pending", "Deposited", or null for All.</summary>
        public static async Task<List<IssueRecordLine>> GetRecordsAsync(string? status, DateTime? dateFrom, DateTime? dateTo)
        {
            var list = new List<IssueRecordLine>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using (var cmd = new SqlCommand("dbo.sp_GetIssueRecords", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                int createdAtOrdinal = -1;
                string[] possibleCols = { "CreatedAt", "Created_At", "CreatedDate", "Created_Date", "CreatedOn", "DateCreated", "CreatedDateTime", "InsertDate", "EntryDate", "Date", "IssueDate", "EventStartDate" };
                foreach (var col in possibleCols)
                {
                    try
                    {
                        createdAtOrdinal = reader.GetOrdinal(col);
                        if (createdAtOrdinal >= 0) break;
                    }
                    catch { }
                }

                while (await reader.ReadAsync())
                {
                    string createdAtStr = "—";
                    if (createdAtOrdinal >= 0 && !reader.IsDBNull(createdAtOrdinal))
                    {
                        var val = reader.GetValue(createdAtOrdinal);
                        if (val is DateTime dt)
                        {
                            createdAtStr = dt.ToString("dd MMM yyyy");
                        }
                        else
                        {
                            var s = val.ToString() ?? "";
                            if (DateTime.TryParse(s, out var parsedDt))
                            {
                                createdAtStr = parsedDt.ToString("dd MMM yyyy");
                            }
                            else if (!string.IsNullOrWhiteSpace(s))
                            {
                                createdAtStr = s;
                            }
                        }
                    }

                    DateTime depositDate = DateTime.MinValue;
                    try
                    {
                        int ord = reader.GetOrdinal("DepositDate");
                        if (ord >= 0 && !reader.IsDBNull(ord))
                        {
                            var val = reader.GetValue(ord);
                            if (val is DateTime dt) depositDate = dt;
                            else if (DateTime.TryParse(val.ToString() ?? "", out var pDt)) depositDate = pDt;
                        }
                    }
                    catch { }

                    list.Add(new IssueRecordLine
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        IssueCode = reader.GetString(reader.GetOrdinal("IssueCode")),
                        EventName = reader.GetString(reader.GetOrdinal("EventName")),
                        Item = reader.GetString(reader.GetOrdinal("Item")),
                        Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                        CreatedAt = createdAtStr,
                        DepositDate = depositDate,
                        ReceiverName = reader.GetString(reader.GetOrdinal("ReceiverName")),
                        ReceiverNumber = reader.GetString(reader.GetOrdinal("ReceiverNumber")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                    });
                }
            }

            // If CreatedAt was missing from stored procedure output, fetch actual CreatedAt timestamp directly from database table
            var unpopulatedIds = new List<int>();
            foreach (var item in list)
            {
                if ((string.IsNullOrWhiteSpace(item.CreatedAt) || item.CreatedAt == "—") && !unpopulatedIds.Contains(item.Id))
                {
                    unpopulatedIds.Add(item.Id);
                }
            }

            if (unpopulatedIds.Count > 0)
            {
                try
                {
                    string idList = string.Join(",", unpopulatedIds);
                    string querySql = $"SELECT Id, CreatedAt FROM dbo.IssueRecords WHERE Id IN ({idList})";
                    using var queryCmd = new SqlCommand(querySql, conn);
                    using var queryReader = await queryCmd.ExecuteReaderAsync();
                    var dateMap = new Dictionary<int, string>();
                    while (await queryReader.ReadAsync())
                    {
                        int id = queryReader.GetInt32(0);
                        if (!queryReader.IsDBNull(1))
                        {
                            var val = queryReader.GetValue(1);
                            if (val is DateTime dt)
                            {
                                dateMap[id] = dt.ToString("dd MMM yyyy");
                            }
                            else if (DateTime.TryParse(val.ToString(), out var pDt))
                            {
                                dateMap[id] = pDt.ToString("dd MMM yyyy");
                            }
                        }
                    }

                    foreach (var line in list)
                    {
                        if (dateMap.TryGetValue(line.Id, out var realDate))
                        {
                            line.CreatedAt = realDate;
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        public static async Task<IssueRecordHeader?> GetHeaderAsync(int id)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            IssueRecordHeader? header = null;
            DateTime? createdAt = null;

            using (var cmd = new SqlCommand("dbo.sp_GetIssueRecordHeader", conn) { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;

                int createdAtOrd = -1;
                try { createdAtOrd = reader.GetOrdinal("CreatedAt"); } catch { }
                if (createdAtOrd >= 0 && !reader.IsDBNull(createdAtOrd))
                {
                    var val = reader.GetValue(createdAtOrd);
                    if (val is DateTime dt) createdAt = dt;
                    else if (DateTime.TryParse(val.ToString(), out var pDt)) createdAt = pDt;
                }

                header = new IssueRecordHeader
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    IssueCode = reader.GetString(reader.GetOrdinal("IssueCode")),
                    EventName = reader.GetString(reader.GetOrdinal("EventName")),
                    EventStartDate = reader.IsDBNull(reader.GetOrdinal("EventStartDate")) ? null : reader.GetDateTime(reader.GetOrdinal("EventStartDate")),
                    EventEndDate = reader.IsDBNull(reader.GetOrdinal("EventEndDate")) ? null : reader.GetDateTime(reader.GetOrdinal("EventEndDate")),
                    DepositDate = reader.GetDateTime(reader.GetOrdinal("DepositDate")),
                    VenueAddress = reader.IsDBNull(reader.GetOrdinal("VenueAddress")) ? null : reader.GetString(reader.GetOrdinal("VenueAddress")),
                    ReceiverName = reader.GetString(reader.GetOrdinal("ReceiverName")),
                    ReceiverNumber = reader.GetString(reader.GetOrdinal("ReceiverNumber")),
                    ReceiverPosition = reader.IsDBNull(reader.GetOrdinal("ReceiverPosition")) ? null : reader.GetString(reader.GetOrdinal("ReceiverPosition")),
                    Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    DepositedOn = reader.IsDBNull(reader.GetOrdinal("DepositedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("DepositedOn")),
                    CreatedAt = createdAt
                };
            }

            if (header != null && !header.CreatedAt.HasValue)
            {
                try
                {
                    using var fallbackCmd = new SqlCommand("SELECT CreatedAt FROM dbo.IssueRecords WHERE Id = @Id", conn);
                    fallbackCmd.Parameters.AddWithValue("@Id", id);
                    var val = await fallbackCmd.ExecuteScalarAsync();
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is DateTime dt) header.CreatedAt = dt;
                        else if (DateTime.TryParse(val.ToString(), out var pDt)) header.CreatedAt = pDt;
                    }
                }
                catch { }
            }

            return header;
        }

        /// <summary>Items for one issue record. Reuses DistributionItemRow since the shape (Category+Product+Qty) matches.</summary>
        public static async Task<List<DistributionItemRow>> GetItemsAsync(int issueRecordId)
        {
            var list = new List<DistributionItemRow>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetIssueRecordItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IssueRecordId", issueRecordId);

            using var reader = await cmd.ExecuteReaderAsync();
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

        /// <summary>Inserts (id == null) or updates the header. Returns (Id, IssueCode).</summary>
        public static async Task<(int Id, string IssueCode)> SaveHeaderAsync(
            int? id, string eventName, DateTime? eventStartDate, DateTime? eventEndDate, DateTime depositDate,
            string? venueAddress, string receiverName, string receiverNumber, string? receiverPosition, string? remark)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SaveIssueRecordHeader", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", (object?)id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EventName", eventName);
            cmd.Parameters.AddWithValue("@EventStartDate", (object?)eventStartDate?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EventEndDate", (object?)eventEndDate?.Date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DepositDate", depositDate.Date);
            cmd.Parameters.AddWithValue("@VenueAddress", (object?)venueAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReceiverName", receiverName);
            cmd.Parameters.AddWithValue("@ReceiverNumber", receiverNumber);
            cmd.Parameters.AddWithValue("@ReceiverPosition", (object?)receiverPosition ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("IssueCode")));
        }

        public static async Task ClearItemsAsync(int issueRecordId)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ClearIssueRecordItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IssueRecordId", issueRecordId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task AddItemAsync(int issueRecordId, string? category, string productName, decimal qty)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddIssueRecordItem", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IssueRecordId", issueRecordId);
            cmd.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductName", productName);
            cmd.Parameters.AddWithValue("@Qty", qty);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task MarkDepositedAsync(int id, DateTime depositedOn)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("dbo.sp_MarkIssueDeposited", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@DepositedOn", depositedOn.Date);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                using var fallbackCmd = new SqlCommand(@"
                    UPDATE dbo.IssueRecords 
                    SET Status = 'Deposited', DepositedOn = @DepositedOn 
                    WHERE Id = @Id", conn);
                fallbackCmd.Parameters.AddWithValue("@Id", id);
                fallbackCmd.Parameters.AddWithValue("@DepositedOn", depositedOn.Date);
                await fallbackCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>Items for the Mark Deposited popup: id + name + issued qty.</summary>
        public static async Task<List<DepositItemEntry>> GetItemsForDepositAsync(int issueRecordId)
        {
            var list = new List<DepositItemEntry>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetIssueRecordItems", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IssueRecordId", issueRecordId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new DepositItemEntry
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    IssuedQty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                });
            }

            return list;
        }

        public static async Task SetItemDepositAsync(int itemId, decimal depositedQty, string? shortfallReason)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("dbo.sp_SetIssueItemDeposit", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@Id", itemId);
                cmd.Parameters.AddWithValue("@DepositedQty", depositedQty);
                cmd.Parameters.AddWithValue("@ShortfallReason", (object?)shortfallReason ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                using var fallbackCmd = new SqlCommand(@"
                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'IssueRecordItems' AND COLUMN_NAME = 'DepositedQty')
                    BEGIN
                        UPDATE dbo.IssueRecordItems 
                        SET DepositedQty = @DepositedQty, ShortfallReason = @ShortfallReason 
                        WHERE Id = @Id
                    END", conn);
                fallbackCmd.Parameters.AddWithValue("@Id", itemId);
                fallbackCmd.Parameters.AddWithValue("@DepositedQty", depositedQty);
                fallbackCmd.Parameters.AddWithValue("@ShortfallReason", (object?)shortfallReason ?? DBNull.Value);
                await fallbackCmd.ExecuteNonQueryAsync();
            }
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
//     public static class IssueRepository
//     {
//         private static string ConnectionString =>
//             ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

//         // ---------------- Catalog ----------------

//         public static async Task<List<IssuableCatalogItem>> GetCatalogAsync()
//         {
//             var list = new List<IssuableCatalogItem>();

//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_GetIssuableCatalog", conn) { CommandType = CommandType.StoredProcedure };
//             using var reader = await cmd.ExecuteReaderAsync();

//             while (await reader.ReadAsync())
//             {
//                 list.Add(new IssuableCatalogItem
//                 {
//                     Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                     ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
//                     ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
//                     ProductCode = reader.GetString(reader.GetOrdinal("ProductCode")),
//                     Category = reader.GetString(reader.GetOrdinal("Category")),
//                     TotalQty = reader.GetDecimal(reader.GetOrdinal("TotalQty")),
//                     IssuedQty = reader.GetDecimal(reader.GetOrdinal("IssuedQty")),
//                     ReservedQty = reader.GetDecimal(reader.GetOrdinal("ReservedQty")),
//                 });
//             }

//             return list;
//         }

//         public static async Task SaveIssuableItemAsync(int productId, decimal totalQty)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_SaveIssuableItem", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@ProductId", productId);
//             cmd.Parameters.AddWithValue("@TotalQty", totalQty);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         // ---------------- Reservations ----------------

//         public static async Task<List<ItemReservation>> GetActiveReservationsAsync()
//         {
//             var list = new List<ItemReservation>();

//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_GetActiveReservations", conn) { CommandType = CommandType.StoredProcedure };
//             using var reader = await cmd.ExecuteReaderAsync();

//             while (await reader.ReadAsync())
//             {
//                 list.Add(new ItemReservation
//                 {
//                     Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                     IssuableItemId = reader.GetInt32(reader.GetOrdinal("IssuableItemId")),
//                     ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
//                     BorrowerName = reader.GetString(reader.GetOrdinal("BorrowerName")),
//                     BorrowerNumber = reader.GetString(reader.GetOrdinal("BorrowerNumber")),
//                     Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
//                     ReservedFrom = reader.GetDateTime(reader.GetOrdinal("ReservedFrom")),
//                     ReservedUntil = reader.IsDBNull(reader.GetOrdinal("ReservedUntil")) ? null : reader.GetDateTime(reader.GetOrdinal("ReservedUntil")),
//                     Status = reader.GetString(reader.GetOrdinal("Status")),
//                 });
//             }

//             return list;
//         }

//         public static async Task<int> AddReservationAsync(
//             int issuableItemId, string borrowerName, string borrowerNumber, decimal qty, DateTime reservedFrom, DateTime? reservedUntil)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_AddReservation", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@IssuableItemId", issuableItemId);
//             cmd.Parameters.AddWithValue("@BorrowerName", borrowerName);
//             cmd.Parameters.AddWithValue("@BorrowerNumber", borrowerNumber);
//             cmd.Parameters.AddWithValue("@Qty", qty);
//             cmd.Parameters.AddWithValue("@ReservedFrom", reservedFrom.Date);
//             cmd.Parameters.AddWithValue("@ReservedUntil", (object?)reservedUntil?.Date ?? DBNull.Value);

//             var result = await cmd.ExecuteScalarAsync();
//             return Convert.ToInt32(result);
//         }

//         public static async Task UpdateReservationStatusAsync(int id, string status)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_UpdateReservationStatus", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@Id", id);
//             cmd.Parameters.AddWithValue("@Status", status);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         // ---------------- Issues ----------------

//         public static async Task<List<ItemIssue>> GetIssuesAsync()
//         {
//             var list = new List<ItemIssue>();

//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_GetIssues", conn) { CommandType = CommandType.StoredProcedure };
//             using var reader = await cmd.ExecuteReaderAsync();

//             while (await reader.ReadAsync())
//             {
//                 list.Add(new ItemIssue
//                 {
//                     Id = reader.GetInt32(reader.GetOrdinal("Id")),
//                     IssueCode = reader.GetString(reader.GetOrdinal("IssueCode")),
//                     IssuableItemId = reader.GetInt32(reader.GetOrdinal("IssuableItemId")),
//                     ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
//                     BorrowerName = reader.GetString(reader.GetOrdinal("BorrowerName")),
//                     BorrowerNumber = reader.GetString(reader.GetOrdinal("BorrowerNumber")),
//                     Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
//                     IssueDate = reader.GetDateTime(reader.GetOrdinal("IssueDate")),
//                     DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
//                     ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReturnDate")),
//                     RenewCount = reader.GetInt32(reader.GetOrdinal("RenewCount")),
//                     Remark = reader.IsDBNull(reader.GetOrdinal("Remark")) ? null : reader.GetString(reader.GetOrdinal("Remark")),
//                     IsOverdue = reader.GetInt32(reader.GetOrdinal("IsOverdue")) == 1,
//                 });
//             }

//             return list;
//         }

//         public static async Task<(int Id, string IssueCode)> AddIssueAsync(
//             int issuableItemId, string borrowerName, string borrowerNumber, decimal qty,
//             DateTime issueDate, DateTime dueDate, string? remark, int? reservationId)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_AddIssue", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@IssuableItemId", issuableItemId);
//             cmd.Parameters.AddWithValue("@BorrowerName", borrowerName);
//             cmd.Parameters.AddWithValue("@BorrowerNumber", borrowerNumber);
//             cmd.Parameters.AddWithValue("@Qty", qty);
//             cmd.Parameters.AddWithValue("@IssueDate", issueDate.Date);
//             cmd.Parameters.AddWithValue("@DueDate", dueDate.Date);
//             cmd.Parameters.AddWithValue("@Remark", (object?)remark ?? DBNull.Value);
//             cmd.Parameters.AddWithValue("@ReservationId", (object?)reservationId ?? DBNull.Value);

//             using var reader = await cmd.ExecuteReaderAsync();
//             await reader.ReadAsync();
//             return (reader.GetInt32(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("IssueCode")));
//         }

//         public static async Task ReturnIssueAsync(int id, DateTime returnDate)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_ReturnIssue", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@Id", id);
//             cmd.Parameters.AddWithValue("@ReturnDate", returnDate.Date);
//             await cmd.ExecuteNonQueryAsync();
//         }

//         public static async Task RenewIssueAsync(int id, DateTime newDueDate)
//         {
//             using var conn = new SqlConnection(ConnectionString);
//             await conn.OpenAsync();

//             using var cmd = new SqlCommand("dbo.sp_RenewIssue", conn) { CommandType = CommandType.StoredProcedure };
//             cmd.Parameters.AddWithValue("@Id", id);
//             cmd.Parameters.AddWithValue("@NewDueDate", newDueDate.Date);
//             await cmd.ExecuteNonQueryAsync();
//         }
//     }
// }
