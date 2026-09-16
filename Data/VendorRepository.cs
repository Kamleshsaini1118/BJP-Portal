using Microsoft.Data.SqlClient;
using StockPortalApp.Helpers;
using StockPortalApp.Models;
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

        public static async Task<List<Vendor>> GetVendorsAsync()
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
                        CreatedAt = createdAtStr
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

        /// <summary>Inserts (vendor.Id == null) or updates (vendor.Id has a value). Returns the vendor's Id.</summary>
        public static async Task<int> SaveVendorAsync(Vendor vendor)
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

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}
