using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Quotations;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class QuotationRepository : IQuotationRepository
    {
        private readonly IDapperContext _context;

        public QuotationRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<QuotationProjectDto>> GetProjectsAsync(string searchKeyword = "")
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT p.[Id], p.[ProjectName] AS Title, p.[Remarks] AS Description, 
                       p.[DateCreated] AS CreatedAt, p.[Status],
                       (SELECT COUNT(1) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]) AS [QuotationCount],
                       (SELECT MIN(q.[QuotationAmount]) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]) AS [LowestAmount]
                FROM [dbo].[QuotationProjects] p
                WHERE (@Search = '' OR p.[ProjectName] LIKE @SearchPattern OR p.[Remarks] LIKE @SearchPattern)
                ORDER BY p.[DateCreated] DESC";

            try
            {
                var items = await conn.QueryAsync<QuotationProjectDto>(
                    sql,
                    new { Search = searchKeyword ?? "", SearchPattern = $"%{searchKeyword}%" });

                return items.AsList();
            }
            catch
            {
                return new List<QuotationProjectDto>();
            }
        }

        public async Task<QuotationProjectDto?> GetProjectByIdAsync(int id)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT p.[Id], p.[ProjectName] AS Title, p.[Remarks] AS Description, 
                       p.[DateCreated] AS CreatedAt, p.[Status],
                       (SELECT COUNT(1) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]) AS [QuotationCount],
                       (SELECT MIN(q.[QuotationAmount]) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]) AS [LowestAmount]
                FROM [dbo].[QuotationProjects] p
                WHERE p.[Id] = @Id";

            try
            {
                var project = await conn.QueryFirstOrDefaultAsync<QuotationProjectDto>(sql, new { Id = id });
                if (project != null)
                {
                    project.Quotations = await GetQuotationsForProjectAsync(id);
                }
                return project;
            }
            catch
            {
                return null;
            }
        }

        public async Task<int> SaveProjectAsync(SaveQuotationProjectDto project)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[QuotationProjects]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[QuotationProjects] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [ProjectName] NVARCHAR(200) NOT NULL,
                        [ClientDepartment] NVARCHAR(200) NULL,
                        [Category] NVARCHAR(100) NULL,
                        [EstimatedBudget] DECIMAL(18,2) NULL,
                        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Open',
                        [Remarks] NVARCHAR(1000) NULL,
                        [DateCreated] DATETIME NOT NULL DEFAULT GETDATE()
                    );
                END

                IF @Id IS NULL OR @Id = 0
                BEGIN
                    INSERT INTO [dbo].[QuotationProjects] ([ProjectName], [Remarks], [Status], [DateCreated])
                    VALUES (@Title, @Description, @Status, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    UPDATE [dbo].[QuotationProjects]
                    SET [ProjectName] = @Title, [Remarks] = @Description, [Status] = @Status
                    WHERE [Id] = @Id;
                    SELECT @Id;
                END";

            return await conn.ExecuteScalarAsync<int>(sql, project);
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync("DELETE FROM [dbo].[VendorQuotations] WHERE [ProjectId] = @Id", new { Id = projectId });
            int rows = await conn.ExecuteAsync("DELETE FROM [dbo].[QuotationProjects] WHERE [Id] = @Id", new { Id = projectId });
            return rows > 0;
        }

        public async Task<List<VendorQuotationDto>> GetQuotationsForProjectAsync(int projectId)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT [Id], [ProjectId], [VendorName], [QuotationAmount], [Remarks], [QuotationFilePath] AS FilePath,
                       [DateReceived] AS SubmissionDate, [Status]
                FROM [dbo].[VendorQuotations]
                WHERE [ProjectId] = @ProjectId
                ORDER BY [QuotationAmount] ASC";

            try
            {
                var items = await conn.QueryAsync<VendorQuotationDto>(sql, new { ProjectId = projectId });
                return items.AsList();
            }
            catch
            {
                return new List<VendorQuotationDto>();
            }
        }

        public async Task<int> SaveQuotationAsync(SaveVendorQuotationDto quotation)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VendorQuotations]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[VendorQuotations] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [ProjectId] INT NOT NULL,
                        [VendorName] NVARCHAR(200) NOT NULL,
                        [QuotationAmount] DECIMAL(18,2) NOT NULL,
                        [Remarks] NVARCHAR(1000) NULL,
                        [QuotationFilePath] NVARCHAR(500) NULL,
                        [DateReceived] DATETIME NOT NULL DEFAULT GETDATE(),
                        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending'
                    );
                END

                IF @Id IS NULL OR @Id = 0
                BEGIN
                    INSERT INTO [dbo].[VendorQuotations] ([ProjectId], [VendorName], [QuotationAmount], [Remarks], [QuotationFilePath], [DateReceived], [Status])
                    VALUES (@ProjectId, @VendorName, @QuotationAmount, @Remarks, @FilePath, @SubmissionDate, @Status);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END
                ELSE
                BEGIN
                    UPDATE [dbo].[VendorQuotations]
                    SET [VendorName] = @VendorName, [QuotationAmount] = @QuotationAmount, [Remarks] = @Remarks, [QuotationFilePath] = @FilePath, [Status] = @Status
                    WHERE [Id] = @Id;
                    SELECT @Id;
                END";

            return await conn.ExecuteScalarAsync<int>(sql, quotation);
        }

        public async Task<bool> UpdateQuotationStatusAsync(int quotationId, string newStatus)
        {
            using var conn = _context.CreateConnection();
            string sql = "UPDATE [dbo].[VendorQuotations] SET [Status] = @Status WHERE [Id] = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Status = newStatus, Id = quotationId });
            return rows > 0;
        }

        public async Task<bool> DeleteQuotationAsync(int quotationId)
        {
            using var conn = _context.CreateConnection();
            string sql = "DELETE FROM [dbo].[VendorQuotations] WHERE [Id] = @Id";
            int rows = await conn.ExecuteAsync(sql, new { Id = quotationId });
            return rows > 0;
        }
    }
}
