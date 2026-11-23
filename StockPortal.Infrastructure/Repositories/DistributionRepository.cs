using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Distributions;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class DistributionRepository : IDistributionRepository
    {
        private readonly IDapperContext _context;

        public DistributionRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<DistributionSummaryDto>> GetDistributionsAsync()
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<DistributionSummaryDto>(
                "dbo.sp_GetDistributions",
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<List<DistributionItemRowDto>> GetItemsAsync(int distributionId)
        {
            using var conn = _context.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "dbo.sp_GetDistributionWithItems",
                new { DistributionId = distributionId },
                commandType: CommandType.StoredProcedure);

            // First result set is header, second is items
            await multi.ReadFirstOrDefaultAsync();
            var items = await multi.ReadAsync<DistributionItemRowDto>();

            return items.AsList();
        }

        public async Task<(int Id, string ShipmentId)> SaveDistributionAsync(SaveDistributionDto dto)
        {
            using var conn = (Microsoft.Data.SqlClient.SqlConnection)_context.CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                int id;
                string shipmentId = dto.ShipmentId ?? "";

                if (dto.Id.HasValue)
                {
                    id = dto.Id.Value;
                    var updateParams = new DynamicParameters();
                    updateParams.Add("@Id", id);
                    updateParams.Add("@Destination", dto.Destination);
                    updateParams.Add("@DistributionDate", dto.Date.Date);
                    updateParams.Add("@ReceiverName", dto.ReceiverName);
                    updateParams.Add("@ReceiverNumber", dto.ReceiverNumber);
                    updateParams.Add("@Position", dto.Position);
                    updateParams.Add("@Remark", dto.Remark);

                    await conn.ExecuteAsync(
                        "dbo.sp_UpdateDistributionHeader",
                        updateParams,
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(
                        "dbo.sp_ClearDistributionItems",
                        new { DistributionId = id },
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);
                }
                else
                {
                    var addParams = new DynamicParameters();
                    addParams.Add("@Destination", dto.Destination);
                    addParams.Add("@DistributionDate", dto.Date.Date);
                    addParams.Add("@ReceiverName", dto.ReceiverName);
                    addParams.Add("@ReceiverNumber", dto.ReceiverNumber);
                    addParams.Add("@Position", dto.Position);
                    addParams.Add("@Remark", dto.Remark);

                    var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "dbo.sp_AddDistribution",
                        addParams,
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);

                    var dict = (IDictionary<string, object>)result;
                    id = Convert.ToInt32(dict["Id"]);
                    shipmentId = dict["ShipmentId"]?.ToString() ?? "";
                }

                foreach (var item in dto.Items)
                {
                    var itemParams = new DynamicParameters();
                    itemParams.Add("@DistributionId", id);
                    itemParams.Add("@Category", item.Category);
                    itemParams.Add("@ProductName", item.ProductName);
                    itemParams.Add("@Qty", item.Qty);

                    await conn.ExecuteAsync(
                        "dbo.sp_AddDistributionItem",
                        itemParams,
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);
                }

                tx.Commit();
                return (id, shipmentId);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<List<DistributionOfficeDto>> GetOfficesAsync()
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<DistributionOfficeDto>(
                "dbo.sp_GetDistributionOffices",
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<DistributionOfficeDto> AddOfficeAsync(string name, string? address, string? contactPerson, string? contactNumber)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Name", name);
            parameters.Add("@Address", address);
            parameters.Add("@ContactPerson", contactPerson);
            parameters.Add("@ContactNumber", contactNumber);

            return await conn.QueryFirstOrDefaultAsync<DistributionOfficeDto>(
                "dbo.sp_AddDistributionOffice",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
