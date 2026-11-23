using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Utilities;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class UtilityRepository : IUtilityRepository
    {
        private readonly IDapperContext _context;

        public UtilityRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<UtilityExpenseDto>> GetExpensesAsync(string? searchQuery = null)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@SearchQuery", string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim());

            try
            {
                var items = await conn.QueryAsync<UtilityExpenseDto>(
                    "dbo.sp_GetUtilityExpenses",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch
            {
                string sql = "SELECT * FROM dbo.UtilityExpenses ORDER BY ExpenseDate DESC";
                var items = await conn.QueryAsync<UtilityExpenseDto>(sql);
                return items.AsList();
            }
        }

        public async Task<int> SaveExpenseAsync(SaveUtilityExpenseDto dto)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Category", dto.Category);
            parameters.Add("@Location", dto.Location);
            parameters.Add("@Amount", dto.Amount);
            parameters.Add("@ExpenseDate", dto.ExpenseDate.Date);
            parameters.Add("@Description", dto.Description);
            parameters.Add("@BillNo", dto.BillNo);
            parameters.Add("@BillImagePath", dto.BillImagePath);

            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    "dbo.sp_AddUtilityExpense",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                string sql = @"
                    INSERT INTO dbo.UtilityExpenses (Category, Location, Amount, ExpenseDate, Description, BillNo, BillImagePath, CreatedAt)
                    VALUES (@Category, @Location, @Amount, @ExpenseDate, @Description, @BillNo, @BillImagePath, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return await conn.ExecuteScalarAsync<int>(sql, dto);
            }
        }

        public async Task<bool> DeleteExpenseAsync(int expenseId)
        {
            using var conn = _context.CreateConnection();
            int rows = await conn.ExecuteAsync("DELETE FROM dbo.UtilityExpenses WHERE Id = @Id", new { Id = expenseId });
            return rows > 0;
        }

        public async Task<List<UtilityReminderDto>> GetRemindersAsync(string? searchQuery = null)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT [Id], [UtilityName] AS UtilityType, 'Headquarters' AS Location, '' AS AccountIdentifier, 
                       [DueDay] AS DueDayOfMonth, 3 AS ReminderDaysBefore, [Remarks] AS Notes, 1 AS IsActive
                FROM [dbo].[UtilityReminders]
                ORDER BY [DueDay] ASC";

            try
            {
                var items = await conn.QueryAsync<UtilityReminderDto>(sql);
                return items.AsList();
            }
            catch
            {
                return new List<UtilityReminderDto>();
            }
        }

        public async Task<int> SaveReminderAsync(SaveUtilityReminderDto dto)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UtilityReminders]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[UtilityReminders] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [UtilityName] NVARCHAR(200) NOT NULL,
                        [DueDay] INT NOT NULL,
                        [RemindDaysBefore] NVARCHAR(50) NULL,
                        [Remarks] NVARCHAR(1000) NULL,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END

                IF @Id IS NULL OR @Id = 0
                BEGIN
                    INSERT INTO dbo.UtilityReminders (UtilityName, DueDay, RemindDaysBefore, Remarks, CreatedAt)
                    VALUES (@UtilityType, @DueDayOfMonth, CAST(@ReminderDaysBefore AS NVARCHAR), @Notes, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    UPDATE dbo.UtilityReminders
                    SET UtilityName = @UtilityType, DueDay = @DueDayOfMonth, RemindDaysBefore = CAST(@ReminderDaysBefore AS NVARCHAR), Remarks = @Notes
                    WHERE Id = @Id;
                    SELECT @Id;
                END";

            return await conn.ExecuteScalarAsync<int>(sql, dto);
        }

        public async Task<bool> DeleteReminderAsync(int reminderId)
        {
            using var conn = _context.CreateConnection();
            int rows = await conn.ExecuteAsync("DELETE FROM dbo.UtilityReminders WHERE Id = @Id", new { Id = reminderId });
            return rows > 0;
        }
    }
}
