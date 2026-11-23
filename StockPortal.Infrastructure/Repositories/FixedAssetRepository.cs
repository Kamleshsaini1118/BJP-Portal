using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.FixedAssets;
using StockPortal.Core.DTOs.Products;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class FixedAssetRepository : IFixedAssetRepository
    {
        private readonly IDapperContext _context;

        public FixedAssetRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<FixedAssetSummaryDto>> GetSummaryAsync()
        {
            try
            {
                using var conn = _context.CreateConnection();
                var items = await conn.QueryAsync<FixedAssetSummaryDto>(
                    "dbo.sp_GetFixedAssetSummary",
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2812)
            {
                // Stored procedure sp_GetFixedAssetSummary does not exist in DB yet - graceful fallback
                return new List<FixedAssetSummaryDto>();
            }
        }

        public async Task<List<AssetDistributionDetailDto>> GetDistributionDetailAsync(string itemName)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<AssetDistributionDetailDto>(
                "dbo.sp_GetAssetDistributionDetail",
                new { ItemName = itemName },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<List<KaryalyaDto>> GetKaryalyasAsync()
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<KaryalyaDto>(
                "dbo.sp_GetKaryalyas",
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<int> SaveKaryalyaAsync(KaryalyaDto karyalya)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Name", karyalya.Name);
            parameters.Add("@Address", karyalya.Address);
            parameters.Add("@InchargeName", karyalya.ContactPerson);
            parameters.Add("@InchargeContact", karyalya.ContactPhone);

            return await conn.ExecuteScalarAsync<int>(
                "dbo.sp_SaveKaryalya",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<DepartmentDto>> GetDepartmentsByKaryalyaAsync(int karyalyaId)
        {
            using var conn = _context.CreateConnection();
            var items = await conn.QueryAsync<DepartmentDto>(
                "dbo.sp_GetDepartmentsByKaryalya",
                new { KaryalyaId = karyalyaId },
                commandType: CommandType.StoredProcedure);

            return items.AsList();
        }

        public async Task<int> SaveDepartmentAsync(DepartmentDto department)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@KaryalyaId", department.KaryalyaId);
            parameters.Add("@Name", department.Name);
            parameters.Add("@InchargeName", department.HeadName);
            parameters.Add("@InchargeContact", department.Phone);

            return await conn.ExecuteScalarAsync<int>(
                "dbo.sp_SaveDepartment",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<FixedAssetRoomDto>> GetFixedAssetRoomsAsync(string? searchQuery = null)
        {
            try
            {
                using var conn = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@SearchQuery", string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim());

                var items = await conn.QueryAsync<FixedAssetRoomDto>(
                    "dbo.sp_GetFixedAssetRooms",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch
            {
                // Fallback direct SQL if stored procedure throws or fails
                using var conn = _context.CreateConnection();
                string sql = @"
                    SELECT 
                        r.[Id] AS [RoomId],
                        r.[Id] AS [Id],
                        r.[KaryalyaId],
                        ISNULL(k.[Name], '') AS [Karyalya],
                        ISNULL(k.[Name], '') AS [KaryalyaName],
                        r.[DepartmentId],
                        ISNULL(d.[Name], '') AS [Department],
                        ISNULL(d.[Name], '') AS [DepartmentName],
                        ISNULL(r.[RoomNo], '') AS [RoomNo],
                        ISNULL(r.[RoomNo], '') AS [RoomNumber],
                        ISNULL(r.[Remarks], '') AS [Remarks],
                        ISNULL(r.[Remarks], '') AS [RoomName],
                        COUNT(ri.[Id]) AS [TotalAssets],
                        COUNT(ri.[Id]) AS [ItemCount],
                        SUM(CASE WHEN ri.[Status] = 'Active' OR ri.[Status] IS NULL THEN 1 ELSE 0 END) AS [ActiveCount],
                        SUM(CASE WHEN ri.[Status] = 'Damaged' THEN 1 ELSE 0 END) AS [DamagedCount],
                        ISNULL(STRING_AGG(ri.[ProductName], ', '), 'No items') AS [ItemsSummary]
                    FROM [dbo].[FixedAssetRooms] r
                    LEFT JOIN [dbo].[Karyalyas] k ON r.[KaryalyaId] = k.[Id]
                    LEFT JOIN [dbo].[Departments] d ON r.[DepartmentId] = d.[Id]
                    LEFT JOIN [dbo].[FixedAssetRoomItems] ri ON r.[Id] = ri.[RoomId]
                    GROUP BY r.[Id], r.[KaryalyaId], k.[Name], r.[DepartmentId], d.[Name], r.[RoomNo], r.[Remarks]
                    ORDER BY r.[Id] DESC";

                var items = await conn.QueryAsync<FixedAssetRoomDto>(sql);
                return items.AsList();
            }
        }

        public async Task<List<ProductDto>> GetFixedAssetProductsListAsync()
        {
            try
            {
                using var conn = _context.CreateConnection();
                var items = await conn.QueryAsync<ProductDto>(
                    "dbo.sp_GetFixedAssetProductsList",
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch
            {
                using var conn = _context.CreateConnection();
                string sql = @"
                    SELECT [Id], [ProductCode], [Name], ISNULL([Category], '') AS [Category], ISNULL([Unit], '') AS [Unit], [ProductType]
                    FROM [dbo].[Products]
                    WHERE [ProductType] = 'Fixed Asset' OR [Category] = 'Fixed Asset'
                    ORDER BY [Name] ASC";

                var items = await conn.QueryAsync<ProductDto>(sql);
                return items.AsList();
            }
        }

        public async Task<List<FixedAssetRoomItemDto>> GetRoomItemsAsync(int roomId)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT 
                    [Id], 
                    [RoomId], 
                    ISNULL([ProductId], 0) AS [ProductId],
                    [ProductName] AS [ItemName], 
                    [ProductName] AS [ProductName],
                    ISNULL([ItemUniqueCode], '') AS [SerialNumber], 
                    ISNULL([ItemUniqueCode], '') AS [ItemUniqueCode],
                    [MappingDate] AS [AssignedDate], 
                    [MappingDate] AS [MappingDate],
                    ISNULL([Status], 'Active') AS [Status]
                FROM [dbo].[FixedAssetRoomItems]
                WHERE [RoomId] = @RoomId
                ORDER BY [Id] ASC";

            var items = await conn.QueryAsync<FixedAssetRoomItemDto>(sql, new { RoomId = roomId });
            return items.AsList();
        }

        public async Task<int> SaveRoomMappingAsync(FixedAssetRoomDto room, List<FixedAssetRoomItemDto> items)
        {
            using var conn = (Microsoft.Data.SqlClient.SqlConnection)_context.CreateConnection();
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();

            try
            {
                int roomId = room.Id;

                if (roomId == 0)
                {
                    string insertRoomSql = @"
                        INSERT INTO [dbo].[FixedAssetRooms] 
                        ([KaryalyaId], [DepartmentId], [RoomNo], [Remarks], [CreatedAt], [UpdatedAt])
                        VALUES (@KaryalyaId, @DepartmentId, @RoomNo, @Remarks, GETDATE(), GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    roomId = await conn.ExecuteScalarAsync<int>(
                        insertRoomSql,
                        new
                        {
                            KaryalyaId = room.KaryalyaId,
                            DepartmentId = room.DepartmentId,
                            RoomNo = room.RoomNumber,
                            Remarks = room.RoomName
                        },
                        transaction: tran);
                }
                else
                {
                    string updateRoomSql = @"
                        UPDATE [dbo].[FixedAssetRooms]
                        SET [KaryalyaId] = @KaryalyaId,
                            [DepartmentId] = @DepartmentId,
                            [RoomNo] = @RoomNo,
                            [Remarks] = @Remarks,
                            [UpdatedAt] = GETDATE()
                        WHERE [Id] = @RoomId;";

                    await conn.ExecuteAsync(
                        updateRoomSql,
                        new
                        {
                            RoomId = roomId,
                            KaryalyaId = room.KaryalyaId,
                            DepartmentId = room.DepartmentId,
                            RoomNo = room.RoomNumber,
                            Remarks = room.RoomName
                        },
                        transaction: tran);

                    await conn.ExecuteAsync(
                        "DELETE FROM [dbo].[FixedAssetRoomItems] WHERE [RoomId] = @RoomId;",
                        new { RoomId = roomId },
                        transaction: tran);
                }

                foreach (var item in items)
                {
                    string insertItemSql = @"
                        INSERT INTO [dbo].[FixedAssetRoomItems]
                        ([RoomId], [ProductId], [ProductName], [ItemUniqueCode], [MappingDate], [Status], [CreatedAt])
                        VALUES (@RoomId, @ProductId, @ProductName, @ItemUniqueCode, @MappingDate, @Status, GETDATE());";

                    await conn.ExecuteAsync(
                        insertItemSql,
                        new
                        {
                            RoomId = roomId,
                            ProductId = item.ProductId > 0 ? item.ProductId : 1,
                            ProductName = !string.IsNullOrWhiteSpace(item.ProductName) ? item.ProductName : item.ItemName,
                            ItemUniqueCode = !string.IsNullOrWhiteSpace(item.ItemUniqueCode) ? item.ItemUniqueCode : (item.SerialNumber ?? Guid.NewGuid().ToString().Substring(0, 8)),
                            MappingDate = item.AssignedDate > DateTime.MinValue ? item.AssignedDate : DateTime.UtcNow,
                            Status = item.Status ?? "Active"
                        },
                        transaction: tran);
                }

                tran.Commit();
                return roomId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<List<FixedAssetReportItemDto>> GetFixedAssetReportItemsAsync()
        {
            using var conn = _context.CreateConnection();
            string query = @"
                SELECT 
                    ri.[Id],
                    ri.[Id] AS [ItemId],
                    ri.[RoomId],
                    ISNULL(ri.[ProductId], 0) AS [ProductId],
                    ISNULL(p.[ProductCode], '') AS [ProductCode],
                    ri.[ProductName] AS [ItemName],
                    ISNULL(ri.[ItemUniqueCode], '') AS [SerialNumber],
                    ISNULL(ri.[ItemUniqueCode], '') AS [ItemUniqueCode],
                    r.[KaryalyaId],
                    ISNULL(k.[Name], '') AS [KaryalyaName],
                    r.[DepartmentId],
                    ISNULL(d.[Name], '') AS [DepartmentName],
                    ISNULL(d.[InchargeName], '') AS [InchargeName],
                    ISNULL(d.[InchargeContact], '') AS [InchargeNumber],
                    ISNULL(d.[InchargeContact], '') AS [InchargeContact],
                    ISNULL(r.[RoomNo], '') AS [RoomNumber],
                    ISNULL(r.[RoomNo], '') AS [RoomNo],
                    ISNULL(r.[Remarks], '') AS [RoomName],
                    ISNULL(r.[Remarks], '') AS [RoomRemarks],
                    ri.[MappingDate] AS [AssignedDate],
                    ri.[MappingDate] AS [MappedOn],
                    ISNULL(ri.[Status], 'Active') AS [Status],
                    ISNULL(r.[UpdatedAt], ri.[CreatedAt]) AS [LastUpdated]
                FROM [dbo].[FixedAssetRoomItems] ri
                INNER JOIN [dbo].[FixedAssetRooms] r ON ri.[RoomId] = r.[Id]
                LEFT JOIN [dbo].[Karyalyas] k ON r.[KaryalyaId] = k.[Id]
                LEFT JOIN [dbo].[Departments] d ON r.[DepartmentId] = d.[Id]
                LEFT JOIN [dbo].[Products] p ON ri.[ProductId] = p.[Id]
                ORDER BY ri.[Id] DESC";

            var items = await conn.QueryAsync<FixedAssetReportItemDto>(query);
            return items.AsList();
        }

        public async Task<List<FixedAssetHistoryRecordDto>> GetItemHistoryAsync(int itemId)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT [Id], [ItemId] AS RoomItemId, [ActionType], [FromLocation], [ToLocation], [Remarks], [ActionDate]
                FROM [dbo].[FixedAssetItemHistory]
                WHERE [ItemId] = @ItemId
                ORDER BY [Id] DESC";

            try
            {
                var items = await conn.QueryAsync<FixedAssetHistoryRecordDto>(sql, new { ItemId = itemId });
                return items.AsList();
            }
            catch
            {
                return new List<FixedAssetHistoryRecordDto>();
            }
        }

        public async Task<bool> MoveAssetItemAsync(int itemId, int toRoomId, string performedBy, string? remarks)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                UPDATE dbo.FixedAssetRoomItems 
                SET RoomId = @ToRoomId 
                WHERE Id = @ItemId";

            int rows = await conn.ExecuteAsync(sql, new { ToRoomId = toRoomId, ItemId = itemId });
            return rows > 0;
        }

        public async Task<bool> UpdateItemStatusAsync(int itemId, string newStatus)
        {
            using var conn = _context.CreateConnection();
            string sql = "UPDATE dbo.FixedAssetRoomItems SET Status = @Status WHERE Id = @ItemId";
            int rows = await conn.ExecuteAsync(sql, new { Status = newStatus, ItemId = itemId });
            return rows > 0;
        }
    }
}