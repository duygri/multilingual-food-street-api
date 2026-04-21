using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicyName)]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            return Ok(new ApiResponse<AuthResponse> { Succeeded = true, Message = "Registration succeeded.", Data = response });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<AuthResponse>
            {
                Succeeded = false,
                Message = "Registration failed.",
                Error = new ErrorResponse { Code = "email_already_registered", Message = ex.Message }
            });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicyName)]
    [HttpPost("register-owner")]
    public async Task<ActionResult<ApiResponse<OwnerRegistrationResponse>>> RegisterOwnerAsync(RegisterOwnerRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.RegisterOwnerAsync(request, cancellationToken);
            return Ok(new ApiResponse<OwnerRegistrationResponse>
            {
                Succeeded = true,
                Message = "Owner application submitted.",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<OwnerRegistrationResponse>
            {
                Succeeded = false,
                Message = "Owner application failed.",
                Error = new ErrorResponse { Code = "email_already_registered", Message = ex.Message }
            });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicyName)]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            return Ok(new ApiResponse<AuthResponse> { Succeeded = true, Message = "Login succeeded.", Data = response });
        }
        catch (AuthFlowException ex)
        {
            return StatusCode((int)ex.StatusCode, new ApiResponse<AuthResponse>
            {
                Succeeded = false,
                Message = "Login blocked.",
                Error = new ErrorResponse { Code = ex.ErrorCode, Message = ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<AuthResponse>
            {
                Succeeded = false,
                Message = "Login failed.",
                Error = new ErrorResponse { Code = "invalid_credentials", Message = ex.Message }
            });
        }
    }

    [AllowAnonymous]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicyName)]
    [HttpPost("login-tourist")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> LoginTouristAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.LoginTouristAsync(request, cancellationToken);
            return Ok(new ApiResponse<AuthResponse> { Succeeded = true, Message = "Tourist login succeeded.", Data = response });
        }
        catch (AuthFlowException ex)
        {
            return StatusCode((int)ex.StatusCode, new ApiResponse<AuthResponse>
            {
                Succeeded = false,
                Message = "Tourist login blocked.",
                Error = new ErrorResponse { Code = ex.ErrorCode, Message = ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<AuthResponse>
            {
                Succeeded = false,
                Message = "Tourist login failed.",
                Error = new ErrorResponse { Code = "invalid_credentials", Message = ex.Message }
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> MeAsync(CancellationToken cancellationToken)
    {
        var response = await authService.GetCurrentUserAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(new ApiResponse<AuthResponse> { Succeeded = true, Message = "Current user loaded.", Data = response });
    }

    [Authorize]
    [EnableRateLimiting(AppConstants.AuthRateLimitPolicyName)]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await authService.ChangePasswordAsync(User.GetRequiredUserId(), request, cancellationToken);
            return Ok(new ApiResponse<object> { Succeeded = true, Message = "Password changed successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Succeeded = false,
                Message = "Password change failed.",
                Error = new ErrorResponse { Code = "invalid_current_password", Message = ex.Message }
            });
        }
    }

    [Authorize]
    [HttpPut("update-profile")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.UpdateProfileAsync(User.GetRequiredUserId(), request, cancellationToken);
        return Ok(new ApiResponse<AuthResponse> { Succeeded = true, Message = "Profile updated.", Data = response });
    }
}
