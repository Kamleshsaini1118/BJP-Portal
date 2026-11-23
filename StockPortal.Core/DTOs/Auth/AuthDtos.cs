using System;

namespace StockPortal.Core.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string IdOrMobile { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? FullName { get; set; }
        public string? MembershipId { get; set; }
        public string? Role { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        public string MembershipId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string Role { get; set; } = "Member";
        public bool IsActive { get; set; }
    }
}
