using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Vendors;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly IDapperContext _context;

        public VendorRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<List<VendorDto>> GetVendorsAsync()
        {
            using var conn = _context.CreateConnection();
            try
            {
                var items = await conn.QueryAsync<VendorDto>(
                    "dbo.sp_GetVendors",
                    commandType: CommandType.StoredProcedure);

                return items.AsList();
            }
            catch
            {
                var fallbackItems = await conn.QueryAsync<VendorDto>(
                    "SELECT Id, Name, Gst, Address, ContactPerson, ContactNumber, Email, BankHolder, BankAccount, BankIfsc, BankBranch, IsActive, CreatedAt FROM dbo.Vendors ORDER BY Name ASC");
                return fallbackItems.AsList();
            }
        }

        public async Task<VendorDto?> GetVendorByIdAsync(int id)
        {
            using var conn = _context.CreateConnection();
            try
            {
                return await conn.QueryFirstOrDefaultAsync<VendorDto>(
                    "dbo.sp_GetVendorById",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);
            }
            catch
            {
                return await conn.QueryFirstOrDefaultAsync<VendorDto>(
                    "SELECT Id, Name, Gst, Address, ContactPerson, ContactNumber, Email, BankHolder, BankAccount, BankIfsc, BankBranch, IsActive, CreatedAt FROM dbo.Vendors WHERE Id = @Id",
                    new { Id = id });
            }
        }

        public async Task<int> SaveVendorAsync(SaveVendorDto vendor)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", vendor.Id);
            parameters.Add("@Name", vendor.Name);
            parameters.Add("@Gst", vendor.Gst);
            parameters.Add("@Address", vendor.Address);
            parameters.Add("@ContactPerson", vendor.ContactPerson);
            parameters.Add("@ContactNumber", vendor.ContactNumber);
            parameters.Add("@Email", vendor.Email);
            parameters.Add("@BankHolder", vendor.BankHolder);
            parameters.Add("@BankAccount", vendor.BankAccount);
            parameters.Add("@BankIfsc", vendor.BankIfsc);
            parameters.Add("@BankBranch", vendor.BankBranch);
            parameters.Add("@IsActive", vendor.IsActive);

            var id = await conn.ExecuteScalarAsync<int>(
                "dbo.sp_SaveVendor",
                parameters,
                commandType: CommandType.StoredProcedure);

            return id;
        }

        public async Task<bool> UpdateVendorAsync(int id, SaveVendorDto vendor)
        {
            vendor.Id = id;
            var resultId = await SaveVendorAsync(vendor);
            return resultId > 0;
        }

        public async Task<bool> ToggleVendorStatusAsync(int id, bool isActive)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            parameters.Add("@IsActive", isActive);

            int affected = await conn.ExecuteAsync(
                "dbo.sp_ToggleVendorStatus",
                parameters,
                commandType: CommandType.StoredProcedure);

            return affected > 0;
        }
    }
}
