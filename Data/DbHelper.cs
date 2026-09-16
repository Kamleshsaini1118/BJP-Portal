using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace StockPortalApp.Data
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string? FullName { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public static class DbHelper
    {
        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["StockPortalDb"].ConnectionString;

        /// <summary>
        /// Validates a Membership ID (or mobile number) + password by calling
        /// dbo.sp_ValidateMemberLogin (see schema.sql).
        /// </summary>
        public static async Task<LoginResult> ValidateLoginAsync(string idOrMobile, string password)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("dbo.sp_ValidateMemberLogin", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@IdOrMobile", idOrMobile);
                cmd.Parameters.AddWithValue("@PasswordHash", Sha256(password));

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return new LoginResult { Success = false, ErrorMessage = "Invalid credentials. Try again." };
                }

                var fullName = reader.GetString(reader.GetOrdinal("FullName"));
                var isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                var passwordMatches = reader.GetInt32(reader.GetOrdinal("PasswordMatches")) == 1;

                if (!isActive)
                {
                    return new LoginResult { Success = false, ErrorMessage = "This account is disabled." };
                }

                if (!passwordMatches)
                {
                    return new LoginResult { Success = false, ErrorMessage = "Invalid credentials. Try again." };
                }

                return new LoginResult { Success = true, FullName = fullName };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = $"Could not reach the database.\n{ex.Message}"
                };
            }
        }

        public static async Task<string> ExecuteSqlAsync(string sql)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            var res = await cmd.ExecuteScalarAsync();
            return res?.ToString() ?? "NULL or Executed";
        }

        private static byte[] Sha256(string input)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        }
    }
}
