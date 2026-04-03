using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FoodStreet.Server.Constants;
using PROJECT_C_.Configuration;
using PROJECT_C_.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PROJECT_C_.Controllers
{
    /// <summary>
    /// Authentication controller - handles register, login, and profile/account operations
    /// </summary>
    [Route("api/content/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        // === DEBUG: Xem claims thực tế trên server ===
        [HttpGet("debug/claims")]
        [Authorize]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                AuthType = User.Identity?.AuthenticationType,
                IsAdmin = User.IsInRole(AppRoles.Admin),
                IsPoiOwner = User.IsInRole(AppRoles.PoiOwner),
                IsTourist = User.IsInRole(AppRoles.Tourist),
                ClaimCount = claims.Count,
                Claims = claims
            });
        }
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse { Success = false, Message = "Vui lòng nhập email và mật khẩu" });
            }

            // Validate Role
            request.Role = AppRoles.NormalizeForPersistence(request.Role);

            var user = new IdentityUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true // Email verified by default for simplicity
            };

            // If POI Owner -> lock account until approved
            if (request.Role == AppRoles.PoiOwner)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Indefinite lock
            }

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Đăng ký thất bại",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            await _userManager.AddToRoleAsync(user, request.Role);
            _logger.LogInformation("User registered: {Email} as {Role}", request.Email, request.Role);

            return Ok(new AuthResponse
            {
                Success = true,
                Message = request.Role == AppRoles.PoiOwner
                    ? "Tài khoản POI Owner đã được tạo. Vui lòng chờ Admin phê duyệt."
                    : "Tài khoản du khách đã được tạo."
            });
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Vui lòng nhập email và mật khẩu"
                });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không đúng"
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

            if (result.IsLockedOut)
            {
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Tài khoản chưa được phê duyệt hoặc đã bị khóa."
                });
            }

            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed - invalid password for: {Email}", request.Email);
                return Unauthorized(new AuthResponse
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không đúng"
                });
            }

            var token = await GenerateJwtToken(user);
            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return Ok(new AuthResponse
            {
                Success = true,
                AccessToken = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                Email = user.Email,
                Message = "Đăng nhập thành công"
            });
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<object>> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                emailConfirmed = user.EmailConfirmed
            });
        }

        /// <summary>
        /// Change password for the currently authenticated user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ mật khẩu" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "Chưa đăng nhập" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản" });

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { success = false, message = "Đổi mật khẩu thất bại", errors });
            }

            _logger.LogInformation("User changed password: {Email}", user.Email);
            return Ok(new { success = true, message = "Đổi mật khẩu thành công" });
        }

        /// <summary>
        /// Update display name for the currently authenticated user
        /// </summary>
        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "Chưa đăng nhập" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản" });

            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                user.UserName = request.DisplayName;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { success = false, message = "Cập nhật thất bại", errors = result.Errors.Select(e => e.Description).ToList() });
                }
            }

            _logger.LogInformation("User updated profile: {Email}", user.Email);
            return Ok(new { success = true, message = "Cập nhật thông tin thành công", displayName = user.UserName });
        }

        #region Private Methods

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim("sub", user.Id),
                new Claim("email", user.Email ?? string.Empty),
                new Claim("name", user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                signingCredentials: credentials
            );

            // Tắt outbound mapping để claim ghi "role" → JWT lưu đúng "role"
            var handler = new JwtSecurityTokenHandler();
            handler.OutboundClaimTypeMap.Clear();
            return handler.WriteToken(token);
        }

        #endregion
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? DisplayName { get; set; }
    }
}
