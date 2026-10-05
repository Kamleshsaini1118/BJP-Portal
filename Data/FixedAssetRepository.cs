using Microsoft.Data.SqlClient;
using StockPortalApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;

namespace StockPortalApp.Data
{
    public static class FixedAssetRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        // ================= KARYALYAS =================
        public static async Task<List<FixedAssetSummary>> GetSummaryAsync()
        {
            var list = new List<FixedAssetSummary>();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("dbo.sp_GetFixedAssetSummary", conn) { CommandType = CommandType.StoredProcedure };
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new FixedAssetSummary
                    {
                        Category = reader.GetString(reader.GetOrdinal("Category")),
                        Item = reader.GetString(reader.GetOrdinal("Item")),
                        Code = reader.GetString(reader.GetOrdinal("Code")),
                        Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
                        Value = reader.GetDecimal(reader.GetOrdinal("Value"))
                    });
                }
            }
            catch { }
            return list;
        }

        public static async Task<List<AssetDistributionDetail>> GetDistributionDetailAsync(string itemName)
        {
            var list = new List<AssetDistributionDetail>();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.sp_GetAssetDistributionDetail", conn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ItemName", itemName);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new AssetDistributionDetail
                    {
                        Destination = reader.IsDBNull(reader.GetOrdinal("Destination")) ? "" : reader.GetString(reader.GetOrdinal("Destination")),
                        Qty = reader.IsDBNull(reader.GetOrdinal("Qty")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Qty")),
                        ReceiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader.GetString(reader.GetOrdinal("ReceiverName")),
                        ReceiverNumber = reader.IsDBNull(reader.GetOrdinal("ReceiverNumber")) ? null : reader.GetString(reader.GetOrdinal("ReceiverNumber")),
                        Position = reader.IsDBNull(reader.GetOrdinal("Position")) ? null : reader.GetString(reader.GetOrdinal("Position")),
                        DistributionDate = reader.IsDBNull(reader.GetOrdinal("DistributionDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("DistributionDate")),
                    });
                }
            }
            catch { }
            return list;
        }

        public static async Task<List<Karyalya>> GetKaryalyasAsync()
        {
            var list = new List<Karyalya>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetKaryalyas", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Karyalya
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Address = reader.GetString(reader.GetOrdinal("Address")),
                    InchargeName = reader.GetString(reader.GetOrdinal("InchargeName")),
                    InchargeContact = reader.GetString(reader.GetOrdinal("InchargeContact")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return list;
        }

        public static async Task<int> SaveKaryalyaAsync(Karyalya karyalya)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SaveKaryalya", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Name", karyalya.Name);
            cmd.Parameters.AddWithValue("@Address", karyalya.Address);
            cmd.Parameters.AddWithValue("@InchargeName", karyalya.InchargeName);
            cmd.Parameters.AddWithValue("@InchargeContact", karyalya.InchargeContact);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // ================= DEPARTMENTS / VIBHAG =================
        public static async Task<List<Department>> GetDepartmentsByKaryalyaAsync(int karyalyaId)
        {
            var list = new List<Department>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetDepartmentsByKaryalya", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@KaryalyaId", karyalyaId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Department
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    KaryalyaId = reader.GetInt32(reader.GetOrdinal("KaryalyaId")),
                    KaryalyaName = reader.GetString(reader.GetOrdinal("KaryalyaName")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    InchargeName = reader.GetString(reader.GetOrdinal("InchargeName")),
                    InchargeContact = reader.GetString(reader.GetOrdinal("InchargeContact")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            return list;
        }

        public static async Task<int> SaveDepartmentAsync(Department department)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SaveDepartment", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@KaryalyaId", department.KaryalyaId);
            cmd.Parameters.AddWithValue("@Name", department.Name);
            cmd.Parameters.AddWithValue("@InchargeName", department.InchargeName);
            cmd.Parameters.AddWithValue("@InchargeContact", department.InchargeContact);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // ================= FIXED ASSET ROOMS (TABLE VIEW) =================
        public static async Task<List<FixedAssetRoom>> GetFixedAssetRoomsAsync(string? searchQuery = null)
        {
            var list = new List<FixedAssetRoom>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetFixedAssetRooms", conn) { CommandType = CommandType.StoredProcedure };
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                cmd.Parameters.AddWithValue("@SearchQuery", searchQuery.Trim());
            }
            else
            {
                cmd.Parameters.AddWithValue("@SearchQuery", DBNull.Value);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new FixedAssetRoom
                {
                    RoomId = reader.GetInt32(reader.GetOrdinal("RoomId")),
                    KaryalyaId = reader.GetInt32(reader.GetOrdinal("KaryalyaId")),
                    Karyalya = reader.GetString(reader.GetOrdinal("Karyalya")),
                    DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
                    Department = reader.GetString(reader.GetOrdinal("Department")),
                    RoomNo = reader.IsDBNull(reader.GetOrdinal("RoomNo")) ? "" : reader.GetString(reader.GetOrdinal("RoomNo")),
                    Remarks = reader.IsDBNull(reader.GetOrdinal("Remarks")) ? null : reader.GetString(reader.GetOrdinal("Remarks")),
                    TotalAssets = reader.IsDBNull(reader.GetOrdinal("TotalAssets")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("TotalAssets"))),
                    ActiveCount = reader.IsDBNull(reader.GetOrdinal("ActiveCount")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ActiveCount"))),
                    DamagedCount = reader.IsDBNull(reader.GetOrdinal("DamagedCount")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("DamagedCount"))),
                    ItemsSummary = reader.IsDBNull(reader.GetOrdinal("ItemsSummary")) ? "No items" : reader.GetString(reader.GetOrdinal("ItemsSummary"))
                });
            }
            return list;
        }

        // ================= FIXED ASSET PRODUCTS LIST =================
        public static async Task<List<Product>> GetFixedAssetProductsListAsync()
        {
            var list = new List<Product>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetFixedAssetProductsList", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Product
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    ProductCode = reader.GetString(reader.GetOrdinal("ProductCode")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString(reader.GetOrdinal("Category")),
                    Unit = reader.IsDBNull(reader.GetOrdinal("Unit")) ? "" : reader.GetString(reader.GetOrdinal("Unit")),
                    ProductType = "Fixed Asset"
                });
            }
            return list;
        }

        // ================= ROOM ITEMS (FOR EDIT & VIEW ASSETS) =================
        public static async Task<List<FixedAssetRoomItem>> GetRoomItemsAsync(int roomId)
        {
            var list = new List<FixedAssetRoomItem>();
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string query = @"SELECT [Id], [RoomId], [ProductId], [ProductName], [ItemUniqueCode], [MappingDate], [Status]
                             FROM [dbo].[FixedAssetRoomItems]
                             WHERE [RoomId] = @RoomId
                             ORDER BY [Id] ASC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@RoomId", roomId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new FixedAssetRoomItem
                {
                    Id = reader.GetInt32(0),
                    RoomId = reader.GetInt32(1),
                    ProductId = reader.GetInt32(2),
                    ProductName = reader.GetString(3),
                    ItemUniqueCode = reader.GetString(4),
                    MappingDate = reader.GetDateTime(5),
                    Status = reader.GetString(6)
                });
            }
            return list;
        }

        // ================= SAVE / UPDATE ROOM MAPPING & ITEMS =================
        public static async Task<int> SaveRoomMappingAsync(FixedAssetRoom room, List<FixedAssetRoomItem> items)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();

            try
            {
                int roomId = room.RoomId;

                if (roomId == 0)
                {
                    string insertRoomSql = @"INSERT INTO [dbo].[FixedAssetRooms] 
                                            ([KaryalyaId], [DepartmentId], [RoomNo], [Remarks], [CreatedAt], [UpdatedAt])
                                            VALUES (@KaryalyaId, @DepartmentId, @RoomNo, @Remarks, GETDATE(), GETDATE());
                                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using var cmd = new SqlCommand(insertRoomSql, conn, tran);
                    cmd.Parameters.AddWithValue("@KaryalyaId", room.KaryalyaId);
                    cmd.Parameters.AddWithValue("@DepartmentId", room.DepartmentId);
                    cmd.Parameters.AddWithValue("@RoomNo", (object?)room.RoomNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Remarks", (object?)room.Remarks ?? DBNull.Value);

                    roomId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                }
                else
                {
                    string updateRoomSql = @"UPDATE [dbo].[FixedAssetRooms]
                                            SET [KaryalyaId] = @KaryalyaId,
                                                [DepartmentId] = @DepartmentId,
                                                [RoomNo] = @RoomNo,
                                                [Remarks] = @Remarks,
                                                [UpdatedAt] = GETDATE()
                                            WHERE [Id] = @RoomId;";

                    using var cmd = new SqlCommand(updateRoomSql, conn, tran);
                    cmd.Parameters.AddWithValue("@RoomId", roomId);
                    cmd.Parameters.AddWithValue("@KaryalyaId", room.KaryalyaId);
                    cmd.Parameters.AddWithValue("@DepartmentId", room.DepartmentId);
                    cmd.Parameters.AddWithValue("@RoomNo", (object?)room.RoomNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Remarks", (object?)room.Remarks ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();

                    // Delete existing items for update to re-insert current items cleanly
                    string deleteItemsSql = "DELETE FROM [dbo].[FixedAssetRoomItems] WHERE [RoomId] = @RoomId;";
                    using var delCmd = new SqlCommand(deleteItemsSql, conn, tran);
                    delCmd.Parameters.AddWithValue("@RoomId", roomId);
                    await delCmd.ExecuteNonQueryAsync();
                }

                // Insert mapped items
                foreach (var item in items)
                {
                    string insertItemSql = @"INSERT INTO [dbo].[FixedAssetRoomItems]
                                            ([RoomId], [ProductId], [ProductName], [ItemUniqueCode], [MappingDate], [Status], [CreatedAt])
                                            VALUES (@RoomId, @ProductId, @ProductName, @ItemUniqueCode, @MappingDate, @Status, GETDATE());";

                    using var itemCmd = new SqlCommand(insertItemSql, conn, tran);
                    itemCmd.Parameters.AddWithValue("@RoomId", roomId);
                    itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                    itemCmd.Parameters.AddWithValue("@ItemUniqueCode", item.ItemUniqueCode);
                    itemCmd.Parameters.AddWithValue("@MappingDate", item.MappingDate);
                    itemCmd.Parameters.AddWithValue("@Status", item.Status ?? "Active");

                    await itemCmd.ExecuteNonQueryAsync();
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

        // ================= FIXED ASSET REPORT =================
        public static async Task<List<FixedAssetReportItem>> GetFixedAssetReportItemsAsync()
        {
            var list = new List<FixedAssetReportItem>();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT 
                                    ri.[Id] AS [ItemId],
                                    ri.[RoomId],
                                    ri.[ProductId],
                                    ri.[ProductName] AS [ItemName],
                                    ISNULL(p.[ProductCode], '') AS [ProductCode],
                                    ri.[ItemUniqueCode],
                                    r.[KaryalyaId],
                                    ISNULL(k.[Name], '') AS [KaryalyaName],
                                    r.[DepartmentId],
                                    ISNULL(d.[Name], '') AS [DepartmentName],
                                    ISNULL(d.[InchargeName], '') AS [InchargeName],
                                    ISNULL(d.[InchargeContact], '') AS [InchargeContact],
                                    ISNULL(r.[RoomNo], '') AS [RoomNo],
                                    ISNULL(r.[Remarks], '') AS [RoomRemarks],
                                    ri.[MappingDate] AS [MappedOn],
                                    ISNULL(ri.[Status], 'Active') AS [Status],
                                    ISNULL(r.[UpdatedAt], ri.[CreatedAt]) AS [LastUpdated]
                                 FROM [dbo].[FixedAssetRoomItems] ri
                                 INNER JOIN [dbo].[FixedAssetRooms] r ON ri.[RoomId] = r.[Id]
                                 LEFT JOIN [dbo].[Karyalyas] k ON r.[KaryalyaId] = k.[Id]
                                 LEFT JOIN [dbo].[Departments] d ON r.[DepartmentId] = d.[Id]
                                 LEFT JOIN [dbo].[Products] p ON ri.[ProductId] = p.[Id]
                                 ORDER BY ri.[Id] DESC";

                using var cmd = new SqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new FixedAssetReportItem
                    {
                        ItemId = reader.GetInt32(reader.GetOrdinal("ItemId")),
                        RoomId = reader.GetInt32(reader.GetOrdinal("RoomId")),
                        ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                        ItemName = reader.IsDBNull(reader.GetOrdinal("ItemName")) ? "" : reader.GetString(reader.GetOrdinal("ItemName")),
                        ProductCode = reader.IsDBNull(reader.GetOrdinal("ProductCode")) ? "" : reader.GetString(reader.GetOrdinal("ProductCode")),
                        ItemUniqueCode = reader.IsDBNull(reader.GetOrdinal("ItemUniqueCode")) ? "" : reader.GetString(reader.GetOrdinal("ItemUniqueCode")),
                        KaryalyaId = reader.GetInt32(reader.GetOrdinal("KaryalyaId")),
                        KaryalyaName = reader.IsDBNull(reader.GetOrdinal("KaryalyaName")) ? "" : reader.GetString(reader.GetOrdinal("KaryalyaName")),
                        DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
                        DepartmentName = reader.IsDBNull(reader.GetOrdinal("DepartmentName")) ? "" : reader.GetString(reader.GetOrdinal("DepartmentName")),
                        InchargeName = reader.IsDBNull(reader.GetOrdinal("InchargeName")) ? "" : reader.GetString(reader.GetOrdinal("InchargeName")),
                        InchargeContact = reader.IsDBNull(reader.GetOrdinal("InchargeContact")) ? "" : reader.GetString(reader.GetOrdinal("InchargeContact")),
                        RoomNo = reader.IsDBNull(reader.GetOrdinal("RoomNo")) ? "" : reader.GetString(reader.GetOrdinal("RoomNo")),
                        RoomRemarks = reader.IsDBNull(reader.GetOrdinal("RoomRemarks")) ? "" : reader.GetString(reader.GetOrdinal("RoomRemarks")),
                        MappedOn = reader.IsDBNull(reader.GetOrdinal("MappedOn")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("MappedOn")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Active" : reader.GetString(reader.GetOrdinal("Status")),
                        LastUpdated = reader.IsDBNull(reader.GetOrdinal("LastUpdated")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("LastUpdated"))
                    });
                }
            }
            catch { }
            return list;
        }

        // ================= ASSET HISTORY & MOVE =================
        private static bool _historyTableChecked = false;

        public static async Task EnsureHistoryTableExistsAsync()
        {
            if (_historyTableChecked) return;
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                string ddl = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FixedAssetItemHistory]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[FixedAssetItemHistory] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [ItemId] INT NOT NULL,
                            [ActionType] NVARCHAR(50) NOT NULL,
                            [FromLocation] NVARCHAR(300) NULL,
                            [ToLocation] NVARCHAR(300) NOT NULL,
                            [Remarks] NVARCHAR(500) NULL,
                            [ActionDate] DATETIME NOT NULL DEFAULT GETDATE(),
                            [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE()
                        );
                    END";
                using var cmd = new SqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync();
                _historyTableChecked = true;
            }
            catch { }
        }

        public static async Task<List<FixedAssetHistoryRecord>> GetItemHistoryAsync(FixedAssetReportItem item)
        {
            var list = new List<FixedAssetHistoryRecord>();
            await EnsureHistoryTableExistsAsync();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT [Id], [ItemId], [ActionType], [FromLocation], [ToLocation], [Remarks], [ActionDate]
                                 FROM [dbo].[FixedAssetItemHistory]
                                 WHERE [ItemId] = @ItemId
                                 ORDER BY [ActionDate] ASC, [Id] ASC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ItemId", item.ItemId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new FixedAssetHistoryRecord
                    {
                        Id = reader.GetInt32(0),
                        ItemId = reader.GetInt32(1),
                        ActionType = reader.GetString(2),
                        FromLocation = reader.IsDBNull(3) ? null : reader.GetString(3),
                        ToLocation = reader.GetString(4),
                        Remarks = reader.IsDBNull(5) ? null : reader.GetString(5),
                        ActionDate = reader.GetDateTime(6)
                    });
                }
            }
            catch { }

            // If no recorded history exists yet, generate initial Mapped record
            if (list.Count == 0)
            {
                string loc = $"{item.KaryalyaName} > {item.DepartmentName}, {item.RoomNoDisplay}".Trim();
                list.Add(new FixedAssetHistoryRecord
                {
                    ItemId = item.ItemId,
                    ActionType = "Mapped",
                    FromLocation = null,
                    ToLocation = loc,
                    Remarks = string.IsNullOrWhiteSpace(item.RoomRemarks) ? null : item.RoomRemarks,
                    ActionDate = item.MappedOn > DateTime.MinValue ? item.MappedOn : DateTime.Today
                });
            }

            return list;
        }

        public static async Task<bool> MoveAssetItemAsync(
            FixedAssetReportItem item,
            int newKaryalyaId,
            string newKaryalyaName,
            int newDepartmentId,
            string newDepartmentName,
            string newRoomNo,
            DateTime moveDate,
            string? remarks)
        {
            await EnsureHistoryTableExistsAsync();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();

            try
            {
                string cleanRoomNo = (newRoomNo ?? "").Trim();

                // 1. Find or create the destination room
                int targetRoomId = 0;
                string findRoomSql = @"SELECT TOP 1 [Id] FROM [dbo].[FixedAssetRooms]
                                       WHERE [KaryalyaId] = @KaryalyaId 
                                         AND [DepartmentId] = @DepartmentId 
                                         AND ISNULL([RoomNo], '') = @RoomNo;";
                using (var findCmd = new SqlCommand(findRoomSql, conn, tran))
                {
                    findCmd.Parameters.AddWithValue("@KaryalyaId", newKaryalyaId);
                    findCmd.Parameters.AddWithValue("@DepartmentId", newDepartmentId);
                    findCmd.Parameters.AddWithValue("@RoomNo", cleanRoomNo);

                    var obj = await findCmd.ExecuteScalarAsync();
                    if (obj != null && obj != DBNull.Value)
                    {
                        targetRoomId = Convert.ToInt32(obj);
                    }
                }

                if (targetRoomId == 0)
                {
                    string insertRoomSql = @"INSERT INTO [dbo].[FixedAssetRooms] 
                                            ([KaryalyaId], [DepartmentId], [RoomNo], [Remarks], [CreatedAt], [UpdatedAt])
                                            VALUES (@KaryalyaId, @DepartmentId, @RoomNo, @Remarks, GETDATE(), GETDATE());
                                            SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    using var insertCmd = new SqlCommand(insertRoomSql, conn, tran);
                    insertCmd.Parameters.AddWithValue("@KaryalyaId", newKaryalyaId);
                    insertCmd.Parameters.AddWithValue("@DepartmentId", newDepartmentId);
                    insertCmd.Parameters.AddWithValue("@RoomNo", string.IsNullOrWhiteSpace(cleanRoomNo) ? DBNull.Value : (object)cleanRoomNo);
                    insertCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : (object)remarks);

                    targetRoomId = (int)(await insertCmd.ExecuteScalarAsync() ?? 0);
                }

                // 2. Update item's RoomId in FixedAssetRoomItems
                string updateItemSql = @"UPDATE [dbo].[FixedAssetRoomItems]
                                         SET [RoomId] = @NewRoomId
                                         WHERE [Id] = @ItemId;";
                using (var updateCmd = new SqlCommand(updateItemSql, conn, tran))
                {
                    updateCmd.Parameters.AddWithValue("@NewRoomId", targetRoomId);
                    updateCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // 3. Check if initial 'Mapped' history exists. If not, record it.
                string checkHistSql = "SELECT COUNT(*) FROM [dbo].[FixedAssetItemHistory] WHERE [ItemId] = @ItemId;";
                int histCount = 0;
                using (var countCmd = new SqlCommand(checkHistSql, conn, tran))
                {
                    countCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    histCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
                }

                string fromLoc = $"{item.KaryalyaName} > {item.DepartmentName}, {item.RoomNoDisplay}".Trim();

                if (histCount == 0)
                {
                    string initHistSql = @"INSERT INTO [dbo].[FixedAssetItemHistory] 
                                           ([ItemId], [ActionType], [FromLocation], [ToLocation], [Remarks], [ActionDate], [CreatedAt])
                                           VALUES (@ItemId, 'Mapped', NULL, @ToLocation, @Remarks, @ActionDate, GETDATE());";
                    using var initCmd = new SqlCommand(initHistSql, conn, tran);
                    initCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    initCmd.Parameters.AddWithValue("@ToLocation", fromLoc);
                    initCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(item.RoomRemarks) ? (object)DBNull.Value : item.RoomRemarks);
                    initCmd.Parameters.AddWithValue("@ActionDate", item.MappedOn > DateTime.MinValue ? item.MappedOn : DateTime.Today);
                    await initCmd.ExecuteNonQueryAsync();
                }

                // 4. Record 'Moved' history entry
                string newRoomDisplay = string.IsNullOrWhiteSpace(cleanRoomNo) ? "-" : (cleanRoomNo.StartsWith("Room", StringComparison.OrdinalIgnoreCase) ? cleanRoomNo : $"Room No. {cleanRoomNo}");
                string toLoc = $"{newKaryalyaName} > {newDepartmentName}, {newRoomDisplay}".Trim();

                string moveHistSql = @"INSERT INTO [dbo].[FixedAssetItemHistory]
                                      ([ItemId], [ActionType], [FromLocation], [ToLocation], [Remarks], [ActionDate], [CreatedAt])
                                      VALUES (@ItemId, 'Moved', @FromLocation, @ToLocation, @Remarks, @ActionDate, GETDATE());";
                using var moveCmd = new SqlCommand(moveHistSql, conn, tran);
                moveCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                moveCmd.Parameters.AddWithValue("@FromLocation", fromLoc);
                moveCmd.Parameters.AddWithValue("@ToLocation", toLoc);
                moveCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(remarks) ? (object)DBNull.Value : remarks);
                moveCmd.Parameters.AddWithValue("@ActionDate", moveDate > DateTime.MinValue ? moveDate : DateTime.Today);
                await moveCmd.ExecuteNonQueryAsync();

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public static async Task<bool> UpdateItemStatusAsync(FixedAssetReportItem item, string newStatus)
        {
            try
            {
                await EnsureHistoryTableExistsAsync();

                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                using var tran = conn.BeginTransaction();

                string sql = "UPDATE [dbo].[FixedAssetRoomItems] SET [Status] = @Status WHERE [Id] = @Id";
                using (var cmd = new SqlCommand(sql, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@Status", newStatus);
                    cmd.Parameters.AddWithValue("@Id", item.ItemId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Check if history table has any records for this item
                string checkHistSql = "SELECT COUNT(*) FROM [dbo].[FixedAssetItemHistory] WHERE [ItemId] = @ItemId;";
                int histCount = 0;
                using (var countCmd = new SqlCommand(checkHistSql, conn, tran))
                {
                    countCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    histCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
                }

                if (histCount == 0)
                {
                    string fromLoc = $"{item.KaryalyaName} > {item.DepartmentName}, {item.RoomNoDisplay}".Trim();
                    string initHistSql = @"INSERT INTO [dbo].[FixedAssetItemHistory] 
                                           ([ItemId], [ActionType], [FromLocation], [ToLocation], [Remarks], [ActionDate], [CreatedAt])
                                           VALUES (@ItemId, 'Mapped', NULL, @ToLocation, @Remarks, @ActionDate, GETDATE());";
                    using var initCmd = new SqlCommand(initHistSql, conn, tran);
                    initCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    initCmd.Parameters.AddWithValue("@ToLocation", fromLoc);
                    initCmd.Parameters.AddWithValue("@Remarks", string.IsNullOrWhiteSpace(item.RoomRemarks) ? (object)DBNull.Value : item.RoomRemarks);
                    initCmd.Parameters.AddWithValue("@ActionDate", item.MappedOn > DateTime.MinValue ? item.MappedOn : DateTime.Today);
                    await initCmd.ExecuteNonQueryAsync();
                }

                string statusHistSql = @"INSERT INTO [dbo].[FixedAssetItemHistory]
                                        ([ItemId], [ActionType], [FromLocation], [ToLocation], [Remarks], [ActionDate], [CreatedAt])
                                        VALUES (@ItemId, 'Status Change', NULL, @ToLocation, NULL, GETDATE(), GETDATE());";
                using (var statusCmd = new SqlCommand(statusHistSql, conn, tran))
                {
                    statusCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    statusCmd.Parameters.AddWithValue("@ToLocation", $"Marked {newStatus}");
                    await statusCmd.ExecuteNonQueryAsync();
                }

                tran.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> UpdateItemStatusAsync(int itemId, string newStatus)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                string sql = "UPDATE [dbo].[FixedAssetRoomItems] SET [Status] = @Status WHERE [Id] = @Id";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Status", newStatus);
                cmd.Parameters.AddWithValue("@Id", itemId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
