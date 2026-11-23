using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockPortal.Core.Common;
using StockPortal.Core.DTOs.Auth;
using StockPortal.Core.Interfaces;

namespace StockPortal.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _hasher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthRepository authRepo,
            ITokenService tokenService,
            IPasswordHasher hasher,
            ILogger<AuthController> logger)
        {
            _authRepo = authRepo;
            _tokenService = tokenService;
            _hasher = hasher;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.IdOrMobile) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ApiResponse<LoginResponseDto>.Fail("Membership ID/Mobile and Password are required."));
            }

            var passwordHash = _hasher.HashSha256(request.Password);
            var (success, fullName, isActive, passwordMatches, role) = await _authRepo.ValidateLoginAsync(request.IdOrMobile, passwordHash);

            if (!success || string.IsNullOrEmpty(fullName))
            {
                _logger.LogWarning("Failed login attempt for user: {User}", request.IdOrMobile);
                return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid credentials. Try again."));
            }

            if (!isActive)
            {
                _logger.LogWarning("Disabled account login attempt: {User}", request.IdOrMobile);
                return Unauthorized(ApiResponse<LoginResponseDto>.Fail("This account is disabled. Contact your administrator."));
            }

            if (!passwordMatches)
            {
                _logger.LogWarning("Incorrect password for user: {User}", request.IdOrMobile);
                return Unauthorized(ApiResponse<LoginResponseDto>.Fail("Invalid credentials. Try again."));
            }

            var token = _tokenService.GenerateAccessToken(request.IdOrMobile, fullName, role ?? "Admin");
            var refreshToken = _tokenService.GenerateRefreshToken();

            _logger.LogInformation("Successful login for user: {User} ({FullName})", request.IdOrMobile, fullName);

            var response = new LoginResponseDto
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(120),
                FullName = fullName,
                MembershipId = request.IdOrMobile,
                Role = role ?? "Admin"
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Login successful."));
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserProfileDto>.Fail("Invalid user identity in token."));
            }

            var profile = await _authRepo.GetUserProfileAsync(userId);
            if (profile == null)
            {
                profile = new UserProfileDto
                {
                    MembershipId = userId,
                    FullName = User.FindFirstValue(ClaimTypes.Name) ?? userId,
                    Role = User.FindFirstValue(ClaimTypes.Role) ?? "Member",
                    IsActive = true
                };
            }

            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }

        [HttpPost("logout")]
        [Authorize]
        public ActionResult<ApiResponse<bool>> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User logged out: {User}", userId);
            return Ok(ApiResponse<bool>.Ok(true, "Logged out successfully."));
        }
    }
}
