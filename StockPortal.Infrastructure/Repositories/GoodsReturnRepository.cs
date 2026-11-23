using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.GoodsReturn;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class GoodsReturnRepository : IGoodsReturnRepository
    {
        private readonly IDapperContext _context;

        public GoodsReturnRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<GoodsReturnDto>> GetGoodsReturnsAsync(
            string? vendor = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? category = null,
            string? productName = null,
            string? reason = null,
            string? searchQuery = null)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@VendorName", string.IsNullOrWhiteSpace(vendor) ? null : vendor);
            parameters.Add("@Category", string.IsNullOrWhiteSpace(category) ? null : category);
            parameters.Add("@ProductName", string.IsNullOrWhiteSpace(productName) ? null : productName);
            parameters.Add("@Reason", string.IsNullOrWhiteSpace(reason) ? null : reason);
            parameters.Add("@DateFrom", dateFrom?.Date);
            parameters.Add("@DateTo", dateTo?.Date);
            parameters.Add("@SearchQuery", string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim());

            var items = await conn.QueryAsync<GoodsReturnDto>(
                "dbo.sp_GetGoodsReturns",
                parameters,
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<(GoodsReturnDto? Header, List<GoodsReturnItemDto> Items)> GetGoodsReturnDetailsAsync(int id)
        {
            using var conn = _context.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "dbo.sp_GetGoodsReturnDetails",
                new { GoodsReturnId = id },
                commandType: CommandType.StoredProcedure);

            var header = await multi.ReadFirstOrDefaultAsync<GoodsReturnDto>();
            var items = (await multi.ReadAsync<GoodsReturnItemDto>()).AsList();

            return (header, items);
        }

        public async Task<List<ReturnablePurchaseOptionDto>> GetVendorPurchasesForReturnAsync(string vendorName)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<ReturnablePurchaseOptionDto>(
                "dbo.sp_GetVendorPurchasesForReturn",
                new { VendorName = vendorName },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<List<ReturnablePurchaseItemRowDto>> GetPurchaseItemsForReturnAsync(int purchaseId)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<ReturnablePurchaseItemRowDto>(
                "dbo.sp_GetPurchaseItemsForReturn",
                new { PurchaseId = purchaseId },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<(int Id, string ReturnNo)> SaveGoodsReturnAsync(SaveGoodsReturnDto dto)
        {
            using var conn = (Microsoft.Data.SqlClient.SqlConnection)_context.CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@ReturnDate", dto.ReturnDate.Date);
                parameters.Add("@VendorName", dto.VendorName);
                parameters.Add("@PurchaseId", dto.PurchaseId);
                parameters.Add("@PurchaseRef", dto.InvoiceOrChallan);
                parameters.Add("@Reason", dto.Reason);
                parameters.Add("@TotalAmount", dto.TotalAmount);
                parameters.Add("@HandedOverTo", null);
                parameters.Add("@ContactNumber", null);
                parameters.Add("@Remarks", dto.Remarks);

                var headerResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "dbo.sp_AddGoodsReturnHeader",
                    parameters,
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                var dict = (IDictionary<string, object>)headerResult;
                int returnId = Convert.ToInt32(dict["Id"]);
                string returnNo = dict["ReturnNo"]?.ToString() ?? "";

                foreach (var item in dto.Items)
                {
                    if (item.ReturnQty <= 0) continue;

                    var itemParams = new DynamicParameters();
                    itemParams.Add("@GoodsReturnId", returnId);
                    itemParams.Add("@PurchaseItemId", null);
                    itemParams.Add("@Category", item.Category);
                    itemParams.Add("@ProductName", item.ProductName);
                    itemParams.Add("@SerialNumber", item.SerialNumber);
                    itemParams.Add("@ReturnQty", item.ReturnQty);
                    itemParams.Add("@Rate", item.UnitPrice);
                    itemParams.Add("@GstPercent", item.GstPercent);

                    await conn.ExecuteAsync(
                        "dbo.sp_AddGoodsReturnItem",
                        itemParams,
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);
                }

                tx.Commit();
                return (returnId, returnNo);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
