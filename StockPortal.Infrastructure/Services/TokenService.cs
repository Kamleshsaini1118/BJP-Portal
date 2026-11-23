using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StockPortal.Core.Interfaces;

namespace StockPortal.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(string membershipId, string fullName, string role)
        {
            var jwtSecret = _config["Jwt:SecretKey"] ?? "DefaultVerySecretKeyForStockPortalApp2026_SecureMinimum32Bytes!";
            var issuer = _config["Jwt:Issuer"] ?? "StockPortalApi";
            var audience = _config["Jwt:Audience"] ?? "StockPortalClients";
            var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var min) ? min : 60;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, membershipId),
                new Claim(JwtRegisteredClaimNames.Name, fullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, membershipId),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(role) ? "Member" : role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
