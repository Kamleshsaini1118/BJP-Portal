using Microsoft.Data.SqlClient;
using StockPortalApp.Models;
using StockPortalApp.Services;
using StockPortal.Core.DTOs.Distributions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPortalApp.Data
{
    public static class OfficeRepository
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        public static async Task<List<DistributionOffice>> GetOfficesAsync()
        {
            // API Call
            var response = await ApiClient.GetAsync<List<DistributionOffice>>("distributions/offices");
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            var list = new List<DistributionOffice>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_GetDistributionOffices", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new DistributionOffice
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    ContactPerson = reader.IsDBNull(reader.GetOrdinal("ContactPerson")) ? null : reader.GetString(reader.GetOrdinal("ContactPerson")),
                    ContactNumber = reader.IsDBNull(reader.GetOrdinal("ContactNumber")) ? null : reader.GetString(reader.GetOrdinal("ContactNumber")),
                });
            }

            return list;
            */
            return new List<DistributionOffice>();
        }

        public static async Task<DistributionOffice> AddOfficeAsync(string name, string? address, string? contactPerson, string? contactNumber)
        {
            // API Call
            var dto = new DistributionOfficeDto
            {
                Name = name,
                Address = address,
                ContactPerson = contactPerson,
                ContactNumber = contactNumber
            };
            var response = await ApiClient.PostAsync<DistributionOffice>("distributions/offices", dto);
            if (response.Success && response.Data != null)
            {
                return response.Data;
            }

            /*
            // --- DIRECT SQL CODE (COMMENTED) ---
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.sp_AddDistributionOffice", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Address", (object?)address ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactPerson", (object?)contactPerson ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactNumber", (object?)contactNumber ?? System.DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new DistributionOffice
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
            };
            */
            return new DistributionOffice { Name = name };
        }
    }
}
