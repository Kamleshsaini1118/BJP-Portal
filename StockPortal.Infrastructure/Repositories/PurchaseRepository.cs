using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Purchases;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly IDapperContext _context;

        public PurchaseRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<PurchaseDto>> GetPurchasesAsync(
            string? vendor = null, DateTime? dateFrom = null, DateTime? dateTo = null,
            string? paymentMode = null, string? poNumber = null, string? filterMode = null)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@VendorName", string.IsNullOrWhiteSpace(vendor) ? null : vendor);
            parameters.Add("@ProductName", null);
            parameters.Add("@DateFrom", dateFrom?.Date);
            parameters.Add("@DateTo", dateTo?.Date);

            var items = await conn.QueryAsync<PurchaseDto>(
                "dbo.sp_GetPurchases",
                parameters,
                commandType: CommandType.StoredProcedure);

            var list = items.AsList();

            if (!string.IsNullOrWhiteSpace(paymentMode))
            {
                list = list.Where(p => string.Equals(p.PaymentMode, paymentMode, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(poNumber))
            {
                list = list.Where(p => p.PoReference != null && p.PoReference.Contains(poNumber, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return list;
        }

        public async Task<List<PurchaseItemDto>> GetPurchaseItemsAsync(int purchaseId)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@PurchaseId", purchaseId);

            var items = await conn.QueryAsync<PurchaseItemDto>(
                "dbo.sp_GetPurchaseItems",
                parameters,
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<int> SavePurchaseAsync(SavePurchaseRequestDto request)
        {
            using var conn = (Microsoft.Data.SqlClient.SqlConnection)_context.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                int? distributionId = null;

                // 1. If Direct purchase, save or update Distribution
                if (string.Equals(request.PurchaseType, "Direct", StringComparison.OrdinalIgnoreCase))
                {
                    var distParams = new DynamicParameters();
                    distParams.Add("@Destination", request.Destination ?? "Direct Delivery");
                    distParams.Add("@DistributionDate", request.DistributionDate?.Date ?? request.PurchaseDate.Date);
                    distParams.Add("@ReceiverName", request.ReceiverName);
                    distParams.Add("@ReceiverNumber", request.ReceiverNumber);
                    distParams.Add("@Position", request.Position);
                    distParams.Add("@Remark", request.DistributionRemark ?? request.Remarks);

                    var distResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "dbo.sp_AddDistribution",
                        distParams,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure);

                    if (distResult != null)
                    {
                        var dict = (IDictionary<string, object>)distResult;
                        distributionId = Convert.ToInt32(dict["Id"]);

                        foreach (var item in request.Items)
                        {
                            var dItemParams = new DynamicParameters();
                            dItemParams.Add("@DistributionId", distributionId.Value);
                            dItemParams.Add("@Category", item.Category);
                            dItemParams.Add("@ProductName", item.ProductName);
                            dItemParams.Add("@Qty", item.Qty);

                            await conn.ExecuteAsync(
                                "dbo.sp_AddDistributionItem",
                                dItemParams,
                                transaction: transaction,
                                commandType: CommandType.StoredProcedure);
                        }
                    }
                }

                // 2. Save Purchase Header
                var pParams = new DynamicParameters();
                pParams.Add("@Id", request.Id);
                pParams.Add("@PurchaseType", request.PurchaseType);
                pParams.Add("@PoReference", request.PoReference);
                pParams.Add("@InvoiceNumber", request.InvoiceNumber);
                pParams.Add("@VendorName", request.VendorName);
                pParams.Add("@PaymentMode", request.PaymentMode);
                pParams.Add("@PurchaseDate", request.PurchaseDate.Date);
                pParams.Add("@ReceiverName", request.ReceiverName);
                pParams.Add("@ReceiverNumber", request.ReceiverNumber);
                pParams.Add("@Remarks", request.Remarks);
                pParams.Add("@DistributionId", distributionId);
                pParams.Add("@ReceiptBasis", request.ReceiptBasis);
                pParams.Add("@ChallanNumber", request.ChallanNumber);
                pParams.Add("@ChallanDate", request.ChallanDate?.Date);
                pParams.Add("@InvoiceDate", request.InvoiceDate?.Date);
                pParams.Add("@InvoiceStatus", request.InvoiceStatus);
                pParams.Add("@InvoiceGroupId", request.InvoiceGroupId);

                var purchaseResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "dbo.sp_SavePurchaseHeader",
                    pParams,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure);

                var pDict = (IDictionary<string, object>)purchaseResult;
                int purchaseId = Convert.ToInt32(pDict["Id"]);

                // 3. Clear existing items and insert fresh items
                if (request.Id.HasValue)
                {
                    await conn.ExecuteAsync(
                        "dbo.sp_ClearPurchaseItems",
                        new { PurchaseId = purchaseId },
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure);
                }

                foreach (var item in request.Items)
                {
                    var itemParams = new DynamicParameters();
                    itemParams.Add("@PurchaseId", purchaseId);
                    itemParams.Add("@Category", item.Category);
                    itemParams.Add("@ProductName", item.ProductName);
                    itemParams.Add("@Qty", item.Qty);
                    itemParams.Add("@Price", item.Price);
                    itemParams.Add("@GstPercent", item.GstPercent);
                    itemParams.Add("@SerialNumber", item.SerialNumber);

                    await conn.ExecuteAsync(
                        "dbo.sp_AddPurchaseItem",
                        itemParams,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure);
                }

                transaction.Commit();
                return purchaseId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> DeleteIncompletePurchaseAsync(int purchaseId, int? distributionId = null)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM dbo.PurchaseInvoiceFiles WHERE PurchaseId = @PurchaseId", new { PurchaseId = purchaseId });
            await conn.ExecuteAsync("DELETE FROM dbo.PurchaseItems WHERE PurchaseId = @PurchaseId", new { PurchaseId = purchaseId });
            await conn.ExecuteAsync("DELETE FROM dbo.Purchases WHERE Id = @PurchaseId", new { PurchaseId = purchaseId });

            if (distributionId.HasValue)
            {
                await conn.ExecuteAsync("DELETE FROM dbo.DistributionItems WHERE DistributionId = @DistId", new { DistId = distributionId.Value });
                await conn.ExecuteAsync("DELETE FROM dbo.Distributions WHERE Id = @DistId", new { DistId = distributionId.Value });
            }

            return true;
        }

        public async Task<List<PendingChallanDto>> GetPendingChallansByVendorAsync(string vendorName)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@VendorName", vendorName);

            var items = await conn.QueryAsync<PendingChallanDto>(
                "dbo.sp_GetPendingChallansByVendor",
                parameters,
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<bool> LinkChallanToInvoiceAsync(int id, string invoiceNumber, DateTime? invoiceDate, int invoiceGroupId, string? appendRemark)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@InvoiceNumber", invoiceNumber);
            parameters.Add("@InvoiceDate", invoiceDate?.Date);
            parameters.Add("@InvoiceGroupId", invoiceGroupId);
            parameters.Add("@AppendRemark", appendRemark);

            int affected = await conn.ExecuteAsync(
                "dbo.sp_LinkChallanToInvoice",
                parameters,
                commandType: CommandType.StoredProcedure);

            return affected > 0;
        }

        public async Task<string?> CheckExistingSerialNumberAsync(IEnumerable<string> serialNumbers, int? currentPurchaseId = null)
        {
            var list = serialNumbers
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0) return null;

            using var conn = _context.CreateConnection();
            foreach (var serial in list)
            {
                string sql = @"
                    SELECT TOP 1 SerialNumber 
                    FROM dbo.PurchaseItems 
                    WHERE SerialNumber = @Serial 
                      AND (@PurchaseId IS NULL OR PurchaseId <> @PurchaseId);";

                var exists = await conn.ExecuteScalarAsync<string>(sql, new { Serial = serial, PurchaseId = currentPurchaseId });
                if (!string.IsNullOrEmpty(exists))
                {
                    return serial;
                }
            }

            return null;
        }

        public async Task<List<InvoiceFileDto>> GetInvoiceFilesAsync(int purchaseId)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<InvoiceFileDto>(
                "dbo.sp_GetPurchaseInvoiceFiles",
                new { PurchaseId = purchaseId },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<int> AddInvoiceFileAsync(int purchaseId, string fileName, byte[] data)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@PurchaseId", purchaseId);
            parameters.Add("@FileName", fileName);
            parameters.Add("@FileData", data);

            return await conn.ExecuteAsync(
                "dbo.sp_AddPurchaseInvoiceFile",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> DeleteInvoiceFileAsync(int fileId)
        {
            using var conn = _context.CreateConnection();
            int rows = await conn.ExecuteAsync(
                "dbo.sp_DeletePurchaseInvoiceFile",
                new { Id = fileId },
                commandType: CommandType.StoredProcedure);

            return rows > 0;
        }
    }
}
