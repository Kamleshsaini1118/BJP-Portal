using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Issues;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class IssueRepository : IIssueRepository
    {
        private readonly IDapperContext _context;

        public IssueRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<IssueRecordLineDto>> GetRecordsAsync(string? status, DateTime? dateFrom, DateTime? dateTo)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Status", string.IsNullOrWhiteSpace(status) ? null : status);
            parameters.Add("@DateFrom", dateFrom?.Date);
            parameters.Add("@DateTo", dateTo?.Date);

            var items = await conn.QueryAsync<IssueRecordLineDto>(
                "dbo.sp_GetIssueRecords",
                parameters,
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<IssueRecordHeaderDto?> GetHeaderAsync(int id)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<IssueRecordHeaderDto>(
                "dbo.sp_GetIssueRecordHeader",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<IssueItemRowDto>> GetItemsAsync(int issueRecordId)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<IssueItemRowDto>(
                "dbo.sp_GetIssueRecordItems",
                new { IssueRecordId = issueRecordId },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<(int Id, string IssueCode)> SaveIssueAsync(SaveIssueDto dto)
        {
            using var conn = (Microsoft.Data.SqlClient.SqlConnection)_context.CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", dto.Id);
                parameters.Add("@EventName", !string.IsNullOrWhiteSpace(dto.EventName) ? dto.EventName : dto.Purpose);
                parameters.Add("@EventStartDate", dto.EventStartDate?.Date ?? (dto.IssueDate != default ? dto.IssueDate.Date : (DateTime?)null));
                parameters.Add("@EventEndDate", dto.EventEndDate?.Date ?? dto.ExpectedReturnDate?.Date);
                parameters.Add("@DepositDate", dto.DepositDate?.Date ?? dto.ExpectedReturnDate?.Date ?? dto.IssueDate.Date);
                parameters.Add("@VenueAddress", dto.VenueAddress);
                parameters.Add("@ReceiverName", !string.IsNullOrWhiteSpace(dto.ReceiverName) ? dto.ReceiverName : dto.IssuedTo);
                parameters.Add("@ReceiverNumber", !string.IsNullOrWhiteSpace(dto.ReceiverNumber) ? dto.ReceiverNumber : (dto.PhoneNumber ?? ""));
                parameters.Add("@ReceiverPosition", dto.ReceiverPosition ?? dto.Designation);
                parameters.Add("@Remark", dto.Remark ?? dto.Remarks);

                var headerResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "dbo.sp_SaveIssueRecordHeader",
                    parameters,
                    transaction: tx,
                    commandType: CommandType.StoredProcedure);

                var dict = (IDictionary<string, object>)headerResult;
                int id = Convert.ToInt32(dict["Id"]);
                string code = dict["IssueCode"]?.ToString() ?? "";

                if (dto.Id.HasValue)
                {
                    await conn.ExecuteAsync(
                        "dbo.sp_ClearIssueRecordItems",
                        new { IssueRecordId = id },
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);
                }

                foreach (var item in dto.Items)
                {
                    var itemParams = new DynamicParameters();
                    itemParams.Add("@IssueRecordId", id);
                    itemParams.Add("@Category", item.Category);
                    itemParams.Add("@ProductName", item.ProductName);
                    itemParams.Add("@Qty", item.Qty);

                    await conn.ExecuteAsync(
                        "dbo.sp_AddIssueRecordItem",
                        itemParams,
                        transaction: tx,
                        commandType: CommandType.StoredProcedure);
                }

                tx.Commit();
                return (id, code);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> MarkDepositedAsync(int id, DateTime depositedOn, List<DepositItemEntryDto> items)
        {
            using var conn = _context.CreateConnection();
            try
            {
                await conn.ExecuteAsync(
                    "dbo.sp_MarkIssueDeposited",
                    new { Id = id, DepositedOn = depositedOn.Date },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                await conn.ExecuteAsync(
                    "UPDATE dbo.IssueRecords SET Status = 'Deposited', DepositedOn = @DepositedOn WHERE Id = @Id",
                    new { Id = id, DepositedOn = depositedOn.Date });
            }

            foreach (var item in items)
            {
                try
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.IssueRecordItems 
                        SET DepositedQty = @DepositedQty, ShortfallReason = @ShortfallReason 
                        WHERE Id = @ItemId",
                        new { ItemId = item.ItemId, DepositedQty = item.DepositedQty, ShortfallReason = item.ShortfallReason });
                }
                catch { }
            }

            return true;
        }
    }
}
