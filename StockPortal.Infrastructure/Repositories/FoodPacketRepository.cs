using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.FoodPackets;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class FoodPacketRepository : IFoodPacketRepository
    {
        private readonly IDapperContext _context;

        public FoodPacketRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<FoodPacketBatchDto>> GetBatchesAsync()
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<FoodPacketBatchDto>(
                "dbo.sp_GetFoodPacketBatches",
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<(int Id, string BatchCode)> AddBatchAsync(CreateFoodPacketBatchDto dto)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@ItemName", dto.ItemName);
            parameters.Add("@MealFor", string.IsNullOrWhiteSpace(dto.MealFor) ? null : dto.MealFor);
            parameters.Add("@VendorName", string.IsNullOrWhiteSpace(dto.VendorName) ? null : dto.VendorName);
            parameters.Add("@MeetingCampaign", string.IsNullOrWhiteSpace(dto.MeetingCampaign) ? null : dto.MeetingCampaign);
            parameters.Add("@Qty", dto.Qty);
            parameters.Add("@BatchDate", dto.BatchDate.Date);
            parameters.Add("@ReceiverName", string.IsNullOrWhiteSpace(dto.ReceiverName) ? null : dto.ReceiverName);
            parameters.Add("@ReceiverNumber", string.IsNullOrWhiteSpace(dto.ReceiverNumber) ? null : dto.ReceiverNumber);

            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "dbo.sp_AddFoodPacketBatch",
                parameters,
                commandType: CommandType.StoredProcedure);

            if (result != null)
            {
                var dict = (IDictionary<string, object>)result;
                int id = Convert.ToInt32(dict["Id"]);
                string code = dict["BatchCode"]?.ToString() ?? "";
                return (id, code);
            }

            return (0, "");
        }

        public async Task<bool> DispatchAsync(int batchId, string personName, string personNumber, decimal qty, DateTime dispatchDate)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@BatchId", batchId);
            parameters.Add("@PersonName", personName);
            parameters.Add("@PersonNumber", personNumber);
            parameters.Add("@Qty", qty);
            parameters.Add("@DispatchDate", dispatchDate.Date);

            int affected = await conn.ExecuteAsync(
                "dbo.sp_DispatchBatch",
                parameters,
                commandType: CommandType.StoredProcedure);

            return affected > 0;
        }

        public async Task<List<BatchDispatchRecordDto>> GetDispatchesAsync(int batchId)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<BatchDispatchRecordDto>(
                "dbo.sp_GetBatchDispatches",
                new { BatchId = batchId },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<List<FoodPacketLedgerDto>> GetLedgerAsync()
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<FoodPacketLedgerDto>(
                "dbo.sp_GetFoodPacketLedger",
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }
    }
}
