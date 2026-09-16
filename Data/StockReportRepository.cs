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
    public static class StockReportRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<StockReportItem>> GetReportAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var list = new List<StockReportItem>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand("dbo.sp_GetStockReport", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@DateFrom", (object?)dateFrom?.Date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DateTo", (object?)dateTo?.Date ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new StockReportItem
                    {
                        Item = reader.GetString(reader.GetOrdinal("Item")),
                        Batches = reader.GetInt32(reader.GetOrdinal("Batches")),
                        TotalQty = reader.GetDecimal(reader.GetOrdinal("TotalQty")),
                        Dispatched = reader.GetDecimal(reader.GetOrdinal("Dispatched")),
                        DamageQty = reader.GetDecimal(reader.GetOrdinal("DamageQty")),
                        IssueQty = reader.GetDecimal(reader.GetOrdinal("IssueQty")),
                        Remaining = reader.GetDecimal(reader.GetOrdinal("Remaining")),
                    });
                }
            }
            catch { }

            // Verify and ensure active pending (non-deposited) issues reduce available remaining stock
            try
            {
                var pendingIssues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                using (var issueCmd = new SqlCommand(@"
                    SELECT ii.ProductName, SUM(ISNULL(ii.Qty, 0)) AS PendingQty
                    FROM dbo.IssueRecordItems ii
                    JOIN dbo.IssueRecords ih ON ii.IssueRecordId = ih.Id
                    WHERE ISNULL(ih.Status, '') <> 'Deposited'
                    GROUP BY ii.ProductName", conn))
                {
                    using var issueReader = await issueCmd.ExecuteReaderAsync();
                    while (await issueReader.ReadAsync())
                    {
                        string pName = issueReader.GetString(0);
                        decimal pQty = issueReader.GetDecimal(1);
                        pendingIssues[pName] = pQty;
                    }
                }

                foreach (var item in list)
                {
                    if (pendingIssues.TryGetValue(item.Item.Trim(), out var activeIssueQty))
                    {
                        item.IssueQty = activeIssueQty;
                    }
                    else
                    {
                        item.IssueQty = 0;
                    }

                    decimal calcRemaining = item.TotalQty - item.Dispatched - item.DamageQty - item.IssueQty;
                    item.Remaining = calcRemaining < 0 ? 0 : calcRemaining;
                }
            }
            catch { }

            return list;
        }

        /// <summary>Full movement history for one product across every source: batches, purchases, dispatches, damage, distributions.</summary>
        public static async Task<List<LedgerEntry>> GetProductLedgerAsync(string productName)
        {
            var list = new List<LedgerEntry>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetProductLedger", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ProductName", productName);

            using var reader = await cmd.ExecuteReaderAsync();

            int dateOrdinal = -1;
            string[] possibleCols = { "MoveDate", "CreatedAt", "Created_At", "CreatedDate", "Created_Date", "CreatedOn", "DateCreated", "Date" };
            foreach (var col in possibleCols)
            {
                try
                {
                    dateOrdinal = reader.GetOrdinal(col);
                    if (dateOrdinal >= 0) break;
                }
                catch { }
            }

            while (await reader.ReadAsync())
            {
                DateTime dt = DateTime.MinValue;
                if (dateOrdinal >= 0 && !reader.IsDBNull(dateOrdinal))
                {
                    var val = reader.GetValue(dateOrdinal);
                    if (val is DateTime dateTimeVal)
                    {
                        dt = dateTimeVal;
                    }
                    else if (DateTime.TryParse(val.ToString(), out var pDt))
                    {
                        dt = pDt;
                    }
                }

                list.Add(new LedgerEntry
                {
                    Type = reader.GetString(reader.GetOrdinal("Type")),
                    BatchCode = reader.GetString(reader.GetOrdinal("Code")),
                    Item = productName,
                    Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                    Party = reader.GetString(reader.GetOrdinal("Party")),
                    CreatedAt = dt,
                });
            }

            return list;
        }
    }
}
