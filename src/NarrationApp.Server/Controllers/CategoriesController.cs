using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Common;

namespace NarrationApp.Server.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetAsync([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var response = await categoryService.GetAllAsync(includeInactive, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<CategoryDto>> { Succeeded = true, Message = "Categories loaded.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.CreateAsync(request, cancellationToken);
        return Ok(new ApiResponse<CategoryDto> { Succeeded = true, Message = "Category created.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(new ApiResponse<CategoryDto> { Succeeded = true, Message = "Category updated.", Data = response });
    }

    [Authorize(Roles = "admin")]
    [EnableRateLimiting(AppConstants.ContentMutationRateLimitPolicyName)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return Ok(new ApiResponse<object> { Succeeded = true, Message = "Category deleted." });
    }
}
