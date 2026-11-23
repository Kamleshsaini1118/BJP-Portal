using System;
using System.Security.Cryptography;
using System.Text;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public byte[] HashSha256(string password)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
