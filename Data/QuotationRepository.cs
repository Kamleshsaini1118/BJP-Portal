using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.Quotations;

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
            // API Call
            var url = "quotations/projects" + (!string.IsNullOrWhiteSpace(searchKeyword) ? $"?searchKeyword={Uri.EscapeDataString(searchKeyword)}" : "");
            var response = await ApiClient.GetAsync<List<QuotationProject>>(url);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
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
                                    Id = reader.GetInt32(0),
                                    ProjectName = reader.GetString(1),
                                    ClientDepartment = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    Category = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Status = reader.IsDBNull(4) ? "Draft" : reader.GetString(4),
                                    DateCreated = reader.GetDateTime(5),
                                    EstimatedBudget = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                    Remarks = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    QuotationCount = reader.GetInt32(8),
                                    LowestQuote = reader.GetDecimal(9)
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
                    var query = InMemProjects.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(searchKeyword))
                    {
                        var s = searchKeyword.Trim().ToLowerInvariant();
                        query = query.Where(p => p.ProjectName.ToLowerInvariant().Contains(s)
                                              || p.ClientDepartment.ToLowerInvariant().Contains(s)
                                              || p.Category.ToLowerInvariant().Contains(s));
                    }
                    return query.OrderByDescending(p => p.DateCreated).ToList();
                }
            }
            */
            return new List<QuotationProject>();
        }

        public static async Task<List<VendorQuotation>> GetQuotationsForProjectAsync(int projectId)
        {
            // API Call
            var response = await ApiClient.GetAsync<List<VendorQuotation>>($"quotations/projects/{projectId}/quotes");
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                var quotes = new List<VendorQuotation>();
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT [Id], [ProjectId], [VendorName], [QuotationReference], 
                               [QuotationAmount], [QuoteDate], [DeliveryDays], [Status], 
                               [IsLowest], [Remarks], [DocumentPath]
                        FROM [dbo].[VendorQuotations]
                        WHERE [ProjectId] = @ProjectId
                        ORDER BY [QuotationAmount] ASC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProjectId", projectId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                quotes.Add(new VendorQuotation
                                {
                                    Id = reader.GetInt32(0),
                                    ProjectId = reader.GetInt32(1),
                                    VendorName = reader.GetString(2),
                                    QuotationReference = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    QuotationAmount = reader.GetDecimal(4),
                                    QuoteDate = reader.GetDateTime(5),
                                    DeliveryDays = reader.IsDBNull(6) ? 7 : reader.GetInt32(6),
                                    Status = reader.IsDBNull(7) ? "Under Review" : reader.GetString(7),
                                    IsLowest = !reader.IsDBNull(8) && reader.GetBoolean(8),
                                    Remarks = reader.IsDBNull(9) ? null : reader.GetString(9),
                                    DocumentPath = reader.IsDBNull(10) ? null : reader.GetString(10)
                                });
                            }
                        }
                    }
                }
                return quotes;
            }
            catch
            {
                lock (SyncLock)
                {
                    return InMemQuotations.Where(q => q.ProjectId == projectId).OrderBy(q => q.QuotationAmount).ToList();
                }
            }
            */
            return new List<VendorQuotation>();
        }

        public static async Task<QuotationProject?> GetProjectByIdAsync(int id)
        {
            // API Call
            var response = await ApiClient.GetAsync<QuotationProject>($"quotations/projects/{id}");
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }
            return null;
        }

        public static async Task<int> SaveProjectAsync(QuotationProject project)
        {
            // API Call
            var dto = new
            {
                Id = project.Id,
                Title = project.ProjectName,
                ProjectName = project.ProjectName,
                ClientDepartment = project.ClientDepartment,
                Category = project.Category,
                Status = project.Status,
                EstimatedBudget = project.EstimatedBudget,
                Remarks = project.Remarks
            };
            var response = await ApiClient.PostAsync<int>("quotations/projects", dto);
            if (response.Success)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    if (project.Id <= 0)
                    {
                        string sql = @"
                            INSERT INTO [dbo].[QuotationProjects]
                            ([ProjectName], [ClientDepartment], [Category], [Status], [DateCreated], [EstimatedBudget], [Remarks])
                            VALUES (@Name, @Dept, @Cat, @Status, @Date, @Budget, @Remarks);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", project.ProjectName);
                            cmd.Parameters.AddWithValue("@Dept", project.ClientDepartment ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Cat", project.Category ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", project.Status ?? "Draft");
                            cmd.Parameters.AddWithValue("@Date", project.DateCreated);
                            cmd.Parameters.AddWithValue("@Budget", project.EstimatedBudget);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)project.Remarks ?? DBNull.Value);

                            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                            project.Id = newId;
                            return newId;
                        }
                    }
                    else
                    {
                        string sql = @"
                            UPDATE [dbo].[QuotationProjects]
                            SET [ProjectName] = @Name,
                                [ClientDepartment] = @Dept,
                                [Category] = @Cat,
                                [Status] = @Status,
                                [EstimatedBudget] = @Budget,
                                [Remarks] = @Remarks
                            WHERE [Id] = @Id";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", project.Id);
                            cmd.Parameters.AddWithValue("@Name", project.ProjectName);
                            cmd.Parameters.AddWithValue("@Dept", project.ClientDepartment ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Cat", project.Category ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", project.Status ?? "Draft");
                            cmd.Parameters.AddWithValue("@Budget", project.EstimatedBudget);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)project.Remarks ?? DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                            return project.Id;
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
                        InMemProjects.Add(project);
                    }
                    else
                    {
                        var existing = InMemProjects.FirstOrDefault(p => p.Id == project.Id);
                        if (existing != null) InMemProjects.Remove(existing);
                        InMemProjects.Add(project);
                    }
                    return project.Id;
                }
            }
            */
            return 0;
        }

        public static async Task<bool> DeleteProjectAsync(int projectId)
        {
            // API Call
            var response = await ApiClient.DeleteAsync<bool>($"quotations/projects/{projectId}");
            if (response.Success)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var tran = conn.BeginTransaction())
                    {
                        string delQuotes = "DELETE FROM [dbo].[VendorQuotations] WHERE [ProjectId] = @ProjectId";
                        using (var cmd1 = new SqlCommand(delQuotes, conn, tran))
                        {
                            cmd1.Parameters.AddWithValue("@ProjectId", projectId);
                            await cmd1.ExecuteNonQueryAsync();
                        }

                        string delProj = "DELETE FROM [dbo].[QuotationProjects] WHERE [Id] = @Id";
                        using (var cmd2 = new SqlCommand(delProj, conn, tran))
                        {
                            cmd2.Parameters.AddWithValue("@Id", projectId);
                            await cmd2.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return true;
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    InMemQuotations.RemoveAll(q => q.ProjectId == projectId);
                    InMemProjects.RemoveAll(p => p.Id == projectId);
                    return true;
                }
            }
            */
            return false;
        }

        public static async Task<int> SaveQuotationAsync(VendorQuotation quote)
        {
            // API Call
            var dto = new
            {
                Id = quote.Id,
                ProjectId = quote.ProjectId,
                VendorName = quote.VendorName,
                QuotationReference = quote.QuotationNumber,
                QuotationAmount = quote.QuotationAmount,
                DateQuoted = quote.QuotationDate,
                DeliveryDays = 7,
                Status = quote.Status,
                IsLowest = quote.IsLowestQuote,
                Remarks = quote.Remarks,
                DocumentPath = quote.FilePath
            };
            var response = await ApiClient.PostAsync<int>("quotations/quotes", dto);
            if (response.Success)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    if (quote.Id <= 0)
                    {
                        string sql = @"
                            INSERT INTO [dbo].[VendorQuotations]
                            ([ProjectId], [VendorName], [QuotationReference], [QuotationAmount], [QuoteDate], [DeliveryDays], [Status], [IsLowest], [Remarks], [DocumentPath])
                            VALUES (@ProjId, @Vendor, @Ref, @Amt, @Date, @Days, @Status, @Lowest, @Remarks, @Doc);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@ProjId", quote.ProjectId);
                            cmd.Parameters.AddWithValue("@Vendor", quote.VendorName);
                            cmd.Parameters.AddWithValue("@Ref", (object?)quote.QuotationReference ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Amt", quote.QuotationAmount);
                            cmd.Parameters.AddWithValue("@Date", quote.QuoteDate);
                            cmd.Parameters.AddWithValue("@Days", quote.DeliveryDays);
                            cmd.Parameters.AddWithValue("@Status", quote.Status ?? "Under Review");
                            cmd.Parameters.AddWithValue("@Lowest", quote.IsLowest);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)quote.Remarks ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Doc", (object?)quote.DocumentPath ?? DBNull.Value);

                            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                            quote.Id = newId;
                        }
                    }
                    else
                    {
                        string sql = @"
                            UPDATE [dbo].[VendorQuotations]
                            SET [VendorName] = @Vendor,
                                [QuotationReference] = @Ref,
                                [QuotationAmount] = @Amt,
                                [QuoteDate] = @Date,
                                [DeliveryDays] = @Days,
                                [Status] = @Status,
                                [IsLowest] = @Lowest,
                                [Remarks] = @Remarks,
                                [DocumentPath] = @Doc
                            WHERE [Id] = @Id";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", quote.Id);
                            cmd.Parameters.AddWithValue("@Vendor", quote.VendorName);
                            cmd.Parameters.AddWithValue("@Ref", (object?)quote.QuotationReference ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Amt", quote.QuotationAmount);
                            cmd.Parameters.AddWithValue("@Date", quote.QuoteDate);
                            cmd.Parameters.AddWithValue("@Days", quote.DeliveryDays);
                            cmd.Parameters.AddWithValue("@Status", quote.Status ?? "Under Review");
                            cmd.Parameters.AddWithValue("@Lowest", quote.IsLowest);
                            cmd.Parameters.AddWithValue("@Remarks", (object?)quote.Remarks ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Doc", (object?)quote.DocumentPath ?? DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Recalculate lowest quote flag for all quotations of this project
                    await RecalculateLowestQuotesAsync(conn, quote.ProjectId);
                    return quote.Id;
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    if (quote.Id <= 0)
                    {
                        quote.Id = InMemQuotations.Count > 0 ? InMemQuotations.Max(q => q.Id) + 1 : 1;
                        InMemQuotations.Add(quote);
                    }
                    else
                    {
                        var existing = InMemQuotations.FirstOrDefault(q => q.Id == quote.Id);
                        if (existing != null) InMemQuotations.Remove(existing);
                        InMemQuotations.Add(quote);
                    }
                    return quote.Id;
                }
            }
            */
            return 0;
        }

        public static async Task<bool> UpdateQuotationStatusAsync(int quotationId, string newStatus)
        {
            // API Call
            var response = await ApiClient.PatchAsync<bool>($"quotations/quotes/{quotationId}/status", newStatus);
            if (response.Success)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
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
                        return await cmd.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    var q = InMemQuotations.FirstOrDefault(x => x.Id == quotationId);
                    if (q != null)
                    {
                        q.Status = newStatus;
                        return true;
                    }
                    return false;
                }
            }
            */
            return false;
        }

        public static async Task<bool> DeleteQuotationAsync(int quotationId)
        {
            // API Call
            var response = await ApiClient.DeleteAsync<bool>($"quotations/quotes/{quotationId}");
            if (response.Success)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    string sql = "DELETE FROM [dbo].[VendorQuotations] WHERE [Id] = @Id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", quotationId);
                        return await cmd.ExecuteNonQueryAsync() > 0;
                    }
                }
            }
            catch
            {
                lock (SyncLock)
                {
                    InMemQuotations.RemoveAll(q => q.Id == quotationId);
                    return true;
                }
            }
            */
            return false;
        }

        private static async Task RecalculateLowestQuotesAsync(SqlConnection conn, int projectId)
        {
            try
            {
                string sql = @"
                    UPDATE [dbo].[VendorQuotations] SET [IsLowest] = 0 WHERE [ProjectId] = @ProjectId;
                    WITH Ranked AS (
                        SELECT TOP 1 [Id] FROM [dbo].[VendorQuotations]
                        WHERE [ProjectId] = @ProjectId
                        ORDER BY [QuotationAmount] ASC
                    )
                    UPDATE [dbo].[VendorQuotations] SET [IsLowest] = 1 WHERE [Id] IN (SELECT [Id] FROM Ranked);";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ProjectId", projectId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }
    }
}
