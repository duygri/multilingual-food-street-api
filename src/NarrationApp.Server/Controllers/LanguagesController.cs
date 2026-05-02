using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Services;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Languages;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/languages")]
public sealed class LanguagesController(IManagedLanguageService managedLanguageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ManagedLanguageDto>>>> GetAsync(CancellationToken cancellationToken)
    {
        var response = await managedLanguageService.GetAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ManagedLanguageDto>>
        {
            Succeeded = true,
            Message = "Managed languages loaded.",
            Data = response
        });
    }
}
