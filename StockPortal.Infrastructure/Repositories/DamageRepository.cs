using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Damage;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class DamageRepository : IDamageRepository
    {
        private readonly IDapperContext _context;

        public DamageRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<DamageRecordDto>> GetRecordsAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@DateFrom", dateFrom?.Date);
            parameters.Add("@DateTo", dateTo?.Date);

            var items = await conn.QueryAsync<DamageRecordDto>(
                "dbo.sp_GetDamageRecords",
                parameters,
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<DamageRecordDto?> GetRecordByIdAsync(int id)
        {
            using var conn = _context.CreateConnection();
            var sql = @"
                SELECT Id, DamageCode, Category, ItemName, Qty, ReporteeName, 
                       ReporteeNumber, ReporteePosition, DamageDate, Remark, ImagePath
                FROM dbo.DamageRecords
                WHERE Id = @Id";

            return await conn.QueryFirstOrDefaultAsync<DamageRecordDto>(sql, new { Id = id });
        }

        public async Task<(int Id, string DamageCode)> AddRecordAsync(CreateDamageRecordDto dto)
        {
            using var conn = _context.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Category", dto.Category);
            parameters.Add("@ItemName", dto.ItemName);
            parameters.Add("@Qty", dto.Qty);
            parameters.Add("@ReporteeName", dto.ReporteeName);
            parameters.Add("@ReporteeNumber", dto.ReporteeNumber);
            parameters.Add("@ReporteePosition", dto.ReporteePosition);
            parameters.Add("@Remark", dto.Remark);

            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "dbo.sp_AddDamageRecord",
                parameters,
                commandType: CommandType.StoredProcedure);

            int id = 0;
            string code = "";

            if (result != null)
            {
                var dict = (IDictionary<string, object>)result;
                if (dict.ContainsKey("Id") && dict["Id"] != null)
                {
                    id = Convert.ToInt32(dict["Id"]);
                }
                if (dict.ContainsKey("DamageCode") && dict["DamageCode"] != null)
                {
                    code = dict["DamageCode"]?.ToString() ?? "";
                }
            }

            string? imagePath = !string.IsNullOrWhiteSpace(dto.ImagePath) ? dto.ImagePath : dto.ImageFileName;
            if (id > 0 && !string.IsNullOrWhiteSpace(imagePath))
            {
                try
                {
                    await conn.ExecuteAsync(
                        "UPDATE dbo.DamageRecords SET ImagePath = @Path WHERE Id = @Id",
                        new { Path = imagePath, Id = id });
                }
                catch { }
            }

            return (id, code);
        }

        public async Task<bool> CheckDuplicateAsync(CreateDamageRecordDto dto)
        {
            using var conn = _context.CreateConnection();
            var exists = await conn.ExecuteScalarAsync<bool>(@"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM dbo.DamageRecords
                    WHERE ItemName = @ItemName
                      AND Qty = @Qty
                      AND CAST(DamageDate AS DATE) = CAST(GETDATE() AS DATE)
                      AND ISNULL(ReporteeName, '') = ISNULL(@ReporteeName, '')
                      AND ISNULL(Remark, '') = ISNULL(@Remark, '')
                ) THEN 1 ELSE 0 END",
                new
                {
                    dto.ItemName,
                    dto.Qty,
                    ReporteeName = dto.ReporteeName ?? "",
                    Remark = dto.Remark ?? ""
                });

            return exists;
        }
    }
}
