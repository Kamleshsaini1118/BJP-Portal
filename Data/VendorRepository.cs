using Microsoft.Data.SqlClient;
using StockPortalApp.Helpers;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.Products;
using StockPortal.Core.DTOs.Vendors;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Data
{
    public static class VendorRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        private static string FormatCreatedAt(object? val)
        {
            if (val == null) return "—";
            if (val is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var s = je.GetString();
                    if (DateTime.TryParse(s, out var parsedDate)) return parsedDate.ToString("dd MMM yyyy");
                    return string.IsNullOrWhiteSpace(s) ? "—" : s;
                }
                if (je.TryGetDateTime(out var dtVal))
                {
                    return dtVal.ToString("dd MMM yyyy");
                }
                return je.ToString();
            }
            if (val is DateTime dateVal) return dateVal.ToString("dd MMM yyyy");
            var str = val.ToString()?.Trim() ?? "";
            if (DateTime.TryParse(str, out var dtFromStr)) return dtFromStr.ToString("dd MMM yyyy");
            return string.IsNullOrWhiteSpace(str) ? "—" : str;
        }

        public static async Task<List<Vendor>> GetVendorsAsync()
        {
            try
            {
                // API Call
                var response = await ApiClient.GetAsync<List<VendorDto>>("vendors");
                if (response.Success && response.Data != null && response.Data.Count > 0)
                {
                    return response.Data.Select(d => new Vendor
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Gst = d.Gst,
                        Address = d.Address,
                        ContactPerson = d.ContactPerson,
                        ContactNumber = d.ContactNumber,
                        Email = d.Email,
                        BankHolder = d.BankHolder,
                        BankAccount = d.BankAccount,
                        BankIfsc = d.BankIfsc,
                        BankBranch = d.BankBranch,
                        CreatedAt = FormatCreatedAt(d.CreatedAt),
                        IsActive = d.IsActive ?? true
                    }).ToList();
                }
            }
            catch { }

            // Fallback to direct SQL
            return await GetVendorsDirectSqlAsync();
        }

        public static async Task<List<Vendor>> GetVendorsDirectSqlAsync()
        {
            var list = new List<Vendor>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using (var cmd = new SqlCommand("dbo.sp_GetVendors", conn) { CommandType = CommandType.StoredProcedure })
            {
                using var reader = await cmd.ExecuteReaderAsync();

                int createdAtOrdinal = -1;
                string[] possibleCols = { "CreatedAt", "Created_At", "CreatedDate", "Created_Date", "CreatedOn", "DateCreated", "CreatedDateTime", "InsertDate", "EntryDate", "Date" };
                foreach (var col in possibleCols)
                {
                    try
                    {
                        createdAtOrdinal = reader.GetOrdinal(col);
                        if (createdAtOrdinal >= 0) break;
                    }
                    catch { }
                }

                while (await reader.ReadAsync())
                {
                    string createdAtStr = "—";
                    if (createdAtOrdinal >= 0 && !reader.IsDBNull(createdAtOrdinal))
                    {
                        var val = reader.GetValue(createdAtOrdinal);
                        if (val is DateTime dt)
                        {
                            createdAtStr = dt.ToString("dd MMM yyyy");
                        }
                        else
                        {
                            var s = val.ToString() ?? "";
                            if (DateTime.TryParse(s, out var parsedDt))
                            {
                                createdAtStr = parsedDt.ToString("dd MMM yyyy");
                            }
                            else if (!string.IsNullOrWhiteSpace(s))
                            {
                                createdAtStr = s;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(createdAtStr))
                    {
                        createdAtStr = "—";
                    }

                    list.Add(new Vendor
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Gst = reader.IsDBNull(reader.GetOrdinal("Gst")) ? null : reader.GetString(reader.GetOrdinal("Gst")),
                        Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                        ContactPerson = reader.IsDBNull(reader.GetOrdinal("ContactPerson")) ? null : reader.GetString(reader.GetOrdinal("ContactPerson")),
                        ContactNumber = reader.IsDBNull(reader.GetOrdinal("ContactNumber")) ? null : reader.GetString(reader.GetOrdinal("ContactNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        BankHolder = reader.IsDBNull(reader.GetOrdinal("BankHolder")) ? null : reader.GetString(reader.GetOrdinal("BankHolder")),
                        BankAccount = reader.IsDBNull(reader.GetOrdinal("BankAccount")) ? null : reader.GetString(reader.GetOrdinal("BankAccount")),
                        BankIfsc = reader.IsDBNull(reader.GetOrdinal("BankIfsc")) ? null : reader.GetString(reader.GetOrdinal("BankIfsc")),
                        BankBranch = reader.IsDBNull(reader.GetOrdinal("BankBranch")) ? null : reader.GetString(reader.GetOrdinal("BankBranch")),
                        CreatedAt = createdAtStr,
                        IsActive = !reader.IsDBNull(reader.GetOrdinal("IsActive")) && reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    });
                }
            }

            var unpopulatedVendors = list.Where(v => string.IsNullOrWhiteSpace(v.CreatedAt) || v.CreatedAt == "—").ToList();
            if (unpopulatedVendors.Count > 0)
            {
                try
                {
                    var ids = unpopulatedVendors.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToList();
                    if (ids.Count > 0)
                    {
                        string idList = string.Join(",", ids);
                        string querySql = $"SELECT Id, CreatedAt FROM dbo.Vendors WHERE Id IN ({idList})";
                        using var queryCmd = new SqlCommand(querySql, conn);
                        using var queryReader = await queryCmd.ExecuteReaderAsync();
                        var dateMap = new Dictionary<int, string>();
                        while (await queryReader.ReadAsync())
                        {
                            int id = queryReader.GetInt32(0);
                            if (!queryReader.IsDBNull(1))
                            {
                                var val = queryReader.GetValue(1);
                                if (val is DateTime dt) dateMap[id] = dt.ToString("dd MMM yyyy");
                                else if (DateTime.TryParse(val.ToString(), out var pDt)) dateMap[id] = pDt.ToString("dd MMM yyyy");
                            }
                        }

                        foreach (var v in list)
                        {
                            if (v.Id.HasValue && dateMap.TryGetValue(v.Id.Value, out var realDate))
                            {
                                v.CreatedAt = realDate;
                            }
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        public static async Task<Vendor?> GetVendorByIdAsync(int id)
        {
            try
            {
                // API Call
                var response = await ApiClient.GetAsync<VendorDto>($"vendors/{id}");
                if (response.Success && response.Data != null)
                {
                    var d = response.Data;
                    return new Vendor
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Gst = d.Gst,
                        Address = d.Address,
                        ContactPerson = d.ContactPerson,
                        ContactNumber = d.ContactNumber,
                        Email = d.Email,
                        BankHolder = d.BankHolder,
                        BankAccount = d.BankAccount,
                        BankIfsc = d.BankIfsc,
                        BankBranch = d.BankBranch,
                        CreatedAt = FormatCreatedAt(d.CreatedAt),
                        IsActive = d.IsActive ?? true
                    };
                }
            }
            catch { }

            // Direct SQL fallback
            var all = await GetVendorsDirectSqlAsync();
            return all.FirstOrDefault(v => v.Id == id);
        }

        /// <summary>Updates an existing vendor using the PUT /api/v1/vendors/{id} endpoint.</summary>
        public static async Task<bool> UpdateVendorAsync(int id, Vendor vendor)
        {
            vendor.Id = id;
            var resultId = await SaveVendorAsync(vendor);
            return resultId > 0;
        }

        /// <summary>Inserts (vendor.Id == null) or updates (vendor.Id has a value). Returns the vendor's Id.</summary>
        public static async Task<int> SaveVendorAsync(Vendor vendor)
        {
            // DTO payload
            var dto = new SaveVendorDto
            {
                Id = vendor.Id,
                Name = vendor.Name,
                Gst = vendor.Gst,
                Address = vendor.Address,
                ContactPerson = vendor.ContactPerson,
                ContactNumber = vendor.ContactNumber,
                Email = vendor.Email,
                BankHolder = vendor.BankHolder,
                BankAccount = vendor.BankAccount,
                BankIfsc = vendor.BankIfsc,
                BankBranch = vendor.BankBranch,
                IsActive = vendor.IsActive
            };

            try
            {
                // If editing existing vendor, call PUT /api/v1/vendors/{id}
                if (vendor.Id.HasValue && vendor.Id.Value > 0)
                {
                    var editResponse = await ApiClient.PutAsync<int>($"vendors/{vendor.Id.Value}", dto);
                    if (editResponse.Success)
                    {
                        return editResponse.Data > 0 ? editResponse.Data : vendor.Id.Value;
                    }
                }
                else
                {
                    // New vendor: call POST /api/v1/vendors
                    var addResponse = await ApiClient.PostAsync<int>("vendors", dto);
                    if (addResponse.Success)
                    {
                        return addResponse.Data;
                    }
                }
            }
            catch { }

            // Fallback to direct SQL
            return await SaveVendorDirectSqlAsync(vendor);
        }

        public static async Task<int> SaveVendorDirectSqlAsync(Vendor vendor)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_SaveVendor", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Id", (object?)vendor.Id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Name", vendor.Name);
            cmd.Parameters.AddWithValue("@Gst", (object?)vendor.Gst ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", (object?)vendor.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactPerson", (object?)vendor.ContactPerson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactNumber", (object?)vendor.ContactNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", (object?)vendor.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BankHolder", (object?)vendor.BankHolder ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BankAccount", (object?)vendor.BankAccount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BankIfsc", (object?)vendor.BankIfsc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BankBranch", (object?)vendor.BankBranch ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", vendor.IsActive);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public static async Task ToggleVendorStatusAsync(int id, bool isActive)
        {
            // API Call
            await ApiClient.PatchAsync<bool>($"vendors/{id}/status", new ToggleStatusDto { IsActive = isActive });

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_ToggleVendorStatus", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@IsActive", isActive);

            await cmd.ExecuteNonQueryAsync();
            */
        }
    }
}
