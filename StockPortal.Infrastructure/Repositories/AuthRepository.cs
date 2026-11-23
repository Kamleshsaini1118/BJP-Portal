using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using StockPortal.Core.DTOs.Auth;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDapperContext _context;

        public AuthRepository(IDapperContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? FullName, bool IsActive, bool PasswordMatches, string? Role)> ValidateLoginAsync(string idOrMobile, byte[] passwordHash)
        {
            using var conn = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@IdOrMobile", idOrMobile);
            parameters.Add("@PasswordHash", passwordHash);

            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "dbo.sp_ValidateMemberLogin",
                parameters,
                commandType: CommandType.StoredProcedure);

            if (result == null)
            {
                return (false, null, false, false, null);
            }

            var dict = (IDictionary<string, object>)result;
            string fullName = dict.ContainsKey("FullName") && dict["FullName"] != null ? dict["FullName"].ToString()! : "";
            bool isActive = dict.ContainsKey("IsActive") && dict["IsActive"] != null && (bool)dict["IsActive"];
            bool passwordMatches = dict.ContainsKey("PasswordMatches") && dict["PasswordMatches"] != null && System.Convert.ToInt32(dict["PasswordMatches"]) == 1;
            string role = dict.ContainsKey("Role") && dict["Role"] != null ? dict["Role"].ToString()! : "Admin";

            return (true, fullName, isActive, passwordMatches, role);
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(string membershipId)
        {
            using var conn = _context.CreateConnection();
            string sql = @"
                SELECT MembershipId, FullName, MobileNumber AS Mobile, 'Admin' AS Role, IsActive 
                FROM dbo.Members 
                WHERE MembershipId = @MembershipId OR MobileNumber = @MembershipId";

            return await conn.QueryFirstOrDefaultAsync<UserProfileDto>(sql, new { MembershipId = membershipId });
        }
    }
}
