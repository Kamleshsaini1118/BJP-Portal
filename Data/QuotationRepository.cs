using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StockPortalApp.Models;

namespace StockPortalApp.Data
{
    public static class QuotationRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"]?.ConnectionString ?? string.Empty;

        // Fallback in-memory cache if database table is unavailable
        private static readonly object SyncLock = new();
        private static readonly List<QuotationProject> InMemProjects = new();
        private static readonly List<VendorQuotation> InMemQuotations = new();

        public static async Task<List<QuotationProject>> GetProjectsAsync(string searchKeyword = "")
        {
            try
            {
                var projects = new List<QuotationProject>();
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT p.[Id], p.[ProjectName], p.[ClientDepartment], p.[Category], 
                               p.[Status], p.[DateCreated], p.[EstimatedBudget], p.[Remarks],
                               (SELECT COUNT(1) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]) AS [QuotationCount],
                               ISNULL((SELECT MIN(q.[QuotationAmount]) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]), 0) AS [LowestQuote]
                        FROM [dbo].[QuotationProjects] p";

                    if (!string.IsNullOrWhiteSpace(searchKeyword))
                    {
                        sql += @" WHERE p.[ProjectName] LIKE @Search 
                                    OR p.[ClientDepartment] LIKE @Search 
                                    OR p.[Category] LIKE @Search 
                                    OR p.[Status] LIKE @Search";
                    }

                    sql += " ORDER BY p.[DateCreated] DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(searchKeyword))
                        {
                            cmd.Parameters.AddWithValue("@Search", $"%{searchKeyword.Trim()}%");
                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                projects.Add(new QuotationProject
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    ProjectName = reader.GetString(reader.GetOrdinal("ProjectName")),
                                    ClientDepartment = reader.GetString(reader.GetOrdinal("ClientDepartment")),
                                    Category = reader.GetString(reader.GetOrdinal("Category")),
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    DateCreated = reader.GetDateTime(reader.GetOrdinal("DateCreated")),
                                    EstimatedBudget = reader.IsDBNull(reader.GetOrdinal("EstimatedBudget")) ? null : reader.GetDecimal(reader.GetOrdinal("EstimatedBudget")),
                                    Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? string.Empty : reader.GetString(reader.GetOrdinal("Remarks")),
                                    QuotationCount = reader.GetInt32(reader.GetOrdinal("QuotationCount")),
                                    LowestQuote = reader.GetDecimal(reader.GetOrdinal("LowestQuote"))
                                });
                            }
                        }
                    }
                }
                return projects;
            }
            catch
            {
                lock (SyncLock)
                {
                    RecalculateInMemMetrics();
                    var query = InMemProjects.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(searchKeyword))
                    {
                        string k = searchKeyword.Trim().ToLowerInvariant();
                        query = query.Where(p =>
                            p.ProjectName.ToLowerInvariant().Contains(k) ||
                            p.ClientDepartment.ToLowerInvariant().Contains(k) ||
                            p.Category.ToLowerInvariant().Contains(k) ||
                            p.Status.ToLowerInvariant().Contains(k));
                    }
                    return query.OrderByDescending(p => p.DateCreated).ToList();
                }
            }
        }

        public static async Task<QuotationProject?> GetProjectByIdAsync(int id)
        {
            var projects = await GetProjectsAsync();
            return projects.FirstOrDefault(p => p.Id == id);
        }

        public static async Task SaveProjectAsync(QuotationProject project)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    if (project.Id <= 0)
                    {
                        string sql = @"
                            INSERT INTO [dbo].[QuotationProjects]
                            ([ProjectName], [ClientDepartment], [Category], [EstimatedBudget], [Status], [Remarks], [DateCreated])
                            VALUES (@ProjectName, @ClientDepartment, @Category, @EstimatedBudget, @Status, @Remarks, GETDATE());
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ProjectName", project.ProjectName);
                            cmd.Parameters.AddWithValue("@ClientDepartment", project.ClientDepartment);
                            cmd.Parameters.AddWithValue("@Category", project.Category);
                            cmd.Parameters.AddWithValue("@EstimatedBudget", (object?)project.EstimatedBudget ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(project.Status) ? "Open" : project.Status);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)project.Remarks ?? DBNull.Value);

                            var newIdObj = await cmd.ExecuteScalarAsync();
                            if (newIdObj != null && Convert.ToInt32(newIdObj) > 0)
                            {
                                project.Id = Convert.ToInt32(newIdObj);
                            }
                        }
                    }
                    else
                    {
                        string sql = @"
                            UPDATE [dbo].[QuotationProjects]
                            SET [ProjectName] = @ProjectName,
                                [ClientDepartment] = @ClientDepartment,
                                [Category] = @Category,
                                [EstimatedBudget] = @EstimatedBudget,
                                [Status] = @Status,
                                [Remarks] = @Remarks
                            WHERE [Id] = @Id";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", project.Id);
                            cmd.Parameters.AddWithValue("@ProjectName", project.ProjectName);
                            cmd.Parameters.AddWithValue("@ClientDepartment", project.ClientDepartment);
                            cmd.Parameters.AddWithValue("@Category", project.Category);
                            cmd.Parameters.AddWithValue("@EstimatedBudget", (object?)project.EstimatedBudget ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", project.Status);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)project.Remarks ?? DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    if (project.Id <= 0)
                    {
                        project.Id = InMemProjects.Count > 0 ? InMemProjects.Max(p => p.Id) + 1 : 1;
                        project.DateCreated = DateTime.Today;
                        InMemProjects.Add(project);
                    }
                    else
                    {
                        var existing = InMemProjects.FirstOrDefault(p => p.Id == project.Id);
                        if (existing != null)
                        {
                            existing.ProjectName = project.ProjectName;
                            existing.ClientDepartment = project.ClientDepartment;
                            existing.Category = project.Category;
                            existing.EstimatedBudget = project.EstimatedBudget;
                            existing.Status = project.Status;
                            existing.Remarks = project.Remarks;
                        }
                    }
                    RecalculateInMemMetrics();
                }
            }
        }

        public static async Task DeleteProjectAsync(int projectId)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM [dbo].[QuotationProjects] WHERE [Id] = @Id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", projectId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    InMemProjects.RemoveAll(p => p.Id == projectId);
                    InMemQuotations.RemoveAll(q => q.ProjectId == projectId);
                }
            }
        }

        public static async Task<List<VendorQuotation>> GetQuotationsForProjectAsync(int projectId)
        {
            try
            {
                var list = new List<VendorQuotation>();
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = @"
                        SELECT q.[Id], q.[ProjectId], p.[ProjectName], q.[VendorName], q.[QuotationNumber], 
                               q.[QuotationAmount], q.[Status], q.[QuotationDate], q.[ValidUntil], q.[Remarks], q.[FilePath], q.[IsLowestQuote]
                        FROM [dbo].[VendorQuotations] q
                        LEFT JOIN [dbo].[QuotationProjects] p ON q.[ProjectId] = p.[Id]
                        WHERE q.[ProjectId] = @ProjectId
                        ORDER BY q.[QuotationAmount] ASC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProjectId", projectId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new VendorQuotation
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    ProjectId = reader.GetInt32(reader.GetOrdinal("ProjectId")),
                                    ProjectName = reader.IsDBNull(reader.GetOrdinal("ProjectName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ProjectName")),
                                    VendorName = reader.GetString(reader.GetOrdinal("VendorName")),
                                    QuotationNumber = reader.GetString(reader.GetOrdinal("QuotationNumber")),
                                    QuotationAmount = reader.GetDecimal(reader.GetOrdinal("QuotationAmount")),
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    QuotationDate = reader.GetDateTime(reader.GetOrdinal("QuotationDate")),
                                    ValidUntil = reader.IsDBNull(reader.GetOrdinal("ValidUntil")) ? null : reader.GetDateTime(reader.GetOrdinal("ValidUntil")),
                                    Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? string.Empty : reader.GetString(reader.GetOrdinal("Remarks")),
                                    FilePath = reader.IsDBNull(reader.GetOrdinal("FilePath")) ? string.Empty : reader.GetString(reader.GetOrdinal("FilePath")),
                                    IsLowestQuote = !reader.IsDBNull(reader.GetOrdinal("IsLowestQuote")) && reader.GetBoolean(reader.GetOrdinal("IsLowestQuote"))
                                });
                            }
                        }
                    }
                }

                if (list.Count > 0)
                {
                    decimal minVal = list.Min(q => q.QuotationAmount);
                    foreach (var q in list)
                    {
                        q.IsLowestQuote = (q.QuotationAmount == minVal);
                    }
                }

                return list;
            }
            catch
            {
                lock (SyncLock)
                {
                    var list = InMemQuotations.Where(q => q.ProjectId == projectId).OrderBy(q => q.QuotationAmount).ToList();
                    if (list.Count > 0)
                    {
                        decimal minVal = list.Min(q => q.QuotationAmount);
                        foreach (var q in list)
                        {
                            q.IsLowestQuote = (q.QuotationAmount == minVal);
                        }
                    }
                    return list;
                }
            }
        }

        public static async Task SaveQuotationAsync(VendorQuotation quotation)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    if (quotation.Id <= 0)
                    {
                        string sql = @"
                            INSERT INTO [dbo].[VendorQuotations]
                            ([ProjectId], [VendorName], [QuotationNumber], [QuotationAmount], [Status], [QuotationDate], [ValidUntil], [Remarks], [FilePath], [IsLowestQuote], [CreatedAt])
                            VALUES (@ProjectId, @VendorName, @QuotationNumber, @QuotationAmount, @Status, @QuotationDate, @ValidUntil, @Remarks, @FilePath, @IsLowestQuote, GETDATE());
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ProjectId", quotation.ProjectId);
                            cmd.Parameters.AddWithValue("@VendorName", quotation.VendorName);
                            cmd.Parameters.AddWithValue("@QuotationNumber", quotation.QuotationNumber);
                            cmd.Parameters.AddWithValue("@QuotationAmount", quotation.QuotationAmount);
                            cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(quotation.Status) ? "Pending Review" : quotation.Status);
                            cmd.Parameters.AddWithValue("@QuotationDate", quotation.QuotationDate);
                            cmd.Parameters.AddWithValue("@ValidUntil", (object?)quotation.ValidUntil ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)quotation.Remarks ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FilePath", (object?)quotation.FilePath ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsLowestQuote", quotation.IsLowestQuote);

                            var newIdObj = await cmd.ExecuteScalarAsync();
                            if (newIdObj != null && Convert.ToInt32(newIdObj) > 0)
                            {
                                quotation.Id = Convert.ToInt32(newIdObj);
                            }
                        }
                    }
                    else
                    {
                        string sql = @"
                            UPDATE [dbo].[VendorQuotations]
                            SET [ProjectId] = @ProjectId,
                                [VendorName] = @VendorName,
                                [QuotationNumber] = @QuotationNumber,
                                [QuotationAmount] = @QuotationAmount,
                                [Status] = @Status,
                                [QuotationDate] = @QuotationDate,
                                [ValidUntil] = @ValidUntil,
                                [Remarks] = @Remarks,
                                [FilePath] = @FilePath,
                                [IsLowestQuote] = @IsLowestQuote
                            WHERE [Id] = @Id";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", quotation.Id);
                            cmd.Parameters.AddWithValue("@ProjectId", quotation.ProjectId);
                            cmd.Parameters.AddWithValue("@VendorName", quotation.VendorName);
                            cmd.Parameters.AddWithValue("@QuotationNumber", quotation.QuotationNumber);
                            cmd.Parameters.AddWithValue("@QuotationAmount", quotation.QuotationAmount);
                            cmd.Parameters.AddWithValue("@Status", quotation.Status);
                            cmd.Parameters.AddWithValue("@QuotationDate", quotation.QuotationDate);
                            cmd.Parameters.AddWithValue("@ValidUntil", (object?)quotation.ValidUntil ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)quotation.Remarks ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FilePath", (object?)quotation.FilePath ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsLowestQuote", quotation.IsLowestQuote);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Recalculate project metrics in SQL
                    string syncSql = @"
                        UPDATE p
                        SET p.[QuotationCount] = (SELECT COUNT(1) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]),
                            p.[LowestQuote] = ISNULL((SELECT MIN(q.[QuotationAmount]) FROM [dbo].[VendorQuotations] q WHERE q.[ProjectId] = p.[Id]), 0)
                        FROM [dbo].[QuotationProjects] p
                        WHERE p.[Id] = @ProjectId";

                    using (var cmdSync = new SqlCommand(syncSql, conn))
                    {
                        cmdSync.Parameters.AddWithValue("@ProjectId", quotation.ProjectId);
                        await cmdSync.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    var proj = InMemProjects.FirstOrDefault(p => p.Id == quotation.ProjectId);
                    if (proj != null)
                    {
                        quotation.ProjectName = proj.ProjectName;
                    }

                    if (quotation.Id <= 0)
                    {
                        quotation.Id = InMemQuotations.Count > 0 ? InMemQuotations.Max(q => q.Id) + 1 : 1;
                        InMemQuotations.Add(quotation);
                    }
                    else
                    {
                        var existing = InMemQuotations.FirstOrDefault(q => q.Id == quotation.Id);
                        if (existing != null)
                        {
                            existing.ProjectId = quotation.ProjectId;
                            existing.ProjectName = quotation.ProjectName;
                            existing.VendorName = quotation.VendorName;
                            existing.QuotationNumber = quotation.QuotationNumber;
                            existing.QuotationAmount = quotation.QuotationAmount;
                            existing.Status = quotation.Status;
                            existing.QuotationDate = quotation.QuotationDate;
                            existing.ValidUntil = quotation.ValidUntil;
                            existing.Remarks = quotation.Remarks;
                            if (!string.IsNullOrWhiteSpace(quotation.FilePath))
                            {
                                existing.FilePath = quotation.FilePath;
                            }
                        }
                    }
                    RecalculateInMemMetrics();
                }
            }
        }

        public static async Task UpdateQuotationStatusAsync(int quotationId, string newStatus)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "UPDATE [dbo].[VendorQuotations] SET [Status] = @Status WHERE [Id] = @Id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", quotationId);
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    var existing = InMemQuotations.FirstOrDefault(q => q.Id == quotationId);
                    if (existing != null)
                    {
                        existing.Status = newStatus;
                    }
                    RecalculateInMemMetrics();
                }
            }
        }

        public static async Task DeleteQuotationAsync(int quotationId)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM [dbo].[VendorQuotations] WHERE [Id] = @Id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", quotationId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    InMemQuotations.RemoveAll(q => q.Id == quotationId);
                    RecalculateInMemMetrics();
                }
            }
        }

        private static void RecalculateInMemMetrics()
        {
            foreach (var proj in InMemProjects)
            {
                var projQuotes = InMemQuotations.Where(q => q.ProjectId == proj.Id).ToList();
                proj.QuotationCount = projQuotes.Count;
                if (projQuotes.Count > 0)
                {
                    decimal minVal = projQuotes.Min(q => q.QuotationAmount);
                    proj.LowestQuote = minVal;
                    foreach (var q in projQuotes)
                    {
                        q.IsLowestQuote = (q.QuotationAmount == minVal);
                    }
                }
                else
                {
                    proj.LowestQuote = 0m;
                }
            }
        }
    }
}
