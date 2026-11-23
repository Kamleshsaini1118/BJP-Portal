using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.PurchaseOrders;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly IDapperContext _context;

        public PurchaseOrderRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<PurchaseOrderDto>> GetOrdersAsync(string? vendor = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? status = null, string? productName = null)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@VendorName", string.IsNullOrWhiteSpace(vendor) ? null : vendor);
            parameters.Add("@ProductName", string.IsNullOrWhiteSpace(productName) ? null : productName);
            parameters.Add("@DateFrom", dateFrom?.Date);
            parameters.Add("@DateTo", dateTo?.Date);

            var items = await conn.QueryAsync<PurchaseOrderDto>(
                "dbo.sp_GetPurchaseOrders",
                parameters,
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<List<PurchaseOrderItemDisplayDto>> GetItemsAsync(int purchaseOrderId)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<PurchaseOrderItemDisplayDto>(
                "dbo.sp_GetPurchaseOrderItems",
                new { PurchaseOrderId = purchaseOrderId },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<(int Id, string PoReference)> SaveOrderAsync(SavePurchaseOrderDto dto)
        {
            using var conn = (Microsoft.Data.SqlClient.SqlConnection)_context.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Header (matches sp_SavePurchaseOrderHeader)
                var headerParams = new DynamicParameters();
                headerParams.Add("@Id", dto.Id);
                headerParams.Add("@VendorName", dto.VendorName);
                headerParams.Add("@Delivery", dto.Delivery?.Date ?? dto.ExpectedDate?.Date);
                headerParams.Add("@Remarks", dto.Remarks);

                var headerResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "dbo.sp_SavePurchaseOrderHeader",
                    headerParams,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure);

                var dict = (IDictionary<string, object>)headerResult;
                int poId = Convert.ToInt32(dict["Id"]);
                string poRef = dict["PoReference"]?.ToString() ?? "";

                // 2. Clear previous items if updating
                if (dto.Id.HasValue)
                {
                    await conn.ExecuteAsync(
                        "dbo.sp_ClearPurchaseOrderItems",
                        new { PurchaseOrderId = poId },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure);
                }

                // 3. Line Items (matches sp_AddPurchaseOrderItem)
                foreach (var item in dto.Items)
                {
                    var itemParams = new DynamicParameters();
                    itemParams.Add("@PurchaseOrderId", poId);
                    itemParams.Add("@Category", item.Category);
                    itemParams.Add("@ProductName", item.ProductName);
                    itemParams.Add("@Qty", item.Qty);
                    itemParams.Add("@Price", item.Price > 0 ? item.Price : item.EstimatedPrice);
                    itemParams.Add("@GstPercent", item.GstPercent);

                    await conn.ExecuteAsync(
                        "dbo.sp_AddPurchaseOrderItem",
                        itemParams,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure);
                }

                transaction.Commit();
                return (poId, poRef);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
