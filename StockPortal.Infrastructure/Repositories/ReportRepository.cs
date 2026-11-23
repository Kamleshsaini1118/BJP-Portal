using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Reports;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly IDapperContext _context;

        public ReportRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<StockReportItemDto>> GetStockReportAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@DateFrom", dateFrom?.Date, DbType.Date);
            parameters.Add("@DateTo", dateTo?.Date, DbType.Date);
            try
            {
                var items = await conn.QueryAsync<StockReportItemDto>(
                    "dbo.sp_GetStockReport",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch
            {
                return new List<StockReportItemDto>(); // <-- If anything fails, it returns 0 rows silently!
            }

            // Replicate pending issues calculation from original desktop app:
            //     var pendingIssues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            //     var issueRecords = await conn.QueryAsync<(string ProductName, decimal PendingQty)>(@"
            // SELECT ii.ProductName, SUM(ISNULL(ii.Qty, 0)) AS PendingQty
            // FROM dbo.IssueRecordItems ii
            // JOIN dbo.IssueRecords ih ON ii.IssueRecordId = ih.Id
            // WHERE ISNULL(ih.Status, '') <> 'Deposited'
            // GROUP BY ii.ProductName");

            //     foreach (var row in issueRecords)
            //     {
            //         pendingIssues[row.ProductName] = row.PendingQty;
            //     }

            //     foreach (var item in items)
            //     {
            //         if (pendingIssues.TryGetValue(item.Item.Trim(), out var activeIssueQty))
            //         {
            //             item.IssueQty = activeIssueQty;
            //         }
            //         else
            //         {
            //             item.IssueQty = 0;
            //         }

            //         decimal calcRemaining = item.TotalQty - item.Dispatched - item.DamageQty - item.IssueQty;
            //         item.Remaining = calcRemaining < 0 ? 0 : calcRemaining;
            //     }

            //     return items;
        }

        public async Task<List<LedgerEntryDto>> GetProductLedgerAsync(string productName)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@ProductName", productName);

            try
            {
                var items = await conn.QueryAsync<LedgerEntryDto>(
                    "dbo.sp_GetProductLedger",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch
            {
                return new List<LedgerEntryDto>();
            }
        }

        public async Task<List<PurchaseItemReportRowDto>> GetPurchaseItemsReportAsync(string? vendor = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? category = null)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT p.Id AS PurchaseId, p.PurchaseDate, p.VendorName, 
                       ISNULL(p.InvoiceNumber, p.ChallanNumber) AS InvoiceOrChallan,
                       pi.ProductName, pi.Category, pi.Qty, pi.Price, pi.GstPercent,
                       (pi.Qty * pi.Price * (1 + pi.GstPercent / 100.0)) AS Total,
                       pi.SerialNumber
                FROM dbo.PurchaseItems pi
                JOIN dbo.Purchases p ON pi.PurchaseId = p.Id
                WHERE (@Vendor IS NULL OR p.VendorName = @Vendor)
                  AND (@Category IS NULL OR pi.Category = @Category)
                  AND (@DateFrom IS NULL OR p.PurchaseDate >= @DateFrom)
                  AND (@DateTo IS NULL OR p.PurchaseDate <= @DateTo)
                ORDER BY p.PurchaseDate DESC, p.Id DESC";

            var items = await conn.QueryAsync<PurchaseItemReportRowDto>(
                sql,
                new { Vendor = vendor, Category = category, DateFrom = dateFrom?.Date, DateTo = dateTo?.Date });

            return items.AsList();
        }
    }
}
