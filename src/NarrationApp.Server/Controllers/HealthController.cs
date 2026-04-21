using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Data;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Server.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Ok(new ApiResponse<object>
        {
            Succeeded = true,
            Message = "NarrationApp server is healthy.",
            Data = new
            {
                status = "ok",
                utcTime = DateTime.UtcNow
            }
        });
    }

    [HttpGet("database")]
    public async Task<ActionResult<ApiResponse<object>>> GetDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<object>
            {
                Succeeded = false,
                Message = "Database connection is not available.",
                Error = new ErrorResponse
                {
                    Code = "database_unavailable",
                    Message = "Unable to connect to PostgreSQL."
                }
            });
        }

        return Ok(new ApiResponse<object>
        {
            Succeeded = true,
            Message = "Database connection is healthy.",
            Data = new
            {
                status = "ok",
                provider = dbContext.Database.ProviderName
            }
        });
    }
}
