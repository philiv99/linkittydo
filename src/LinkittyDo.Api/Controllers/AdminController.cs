using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        return Ok(new { data = stats });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isSimulated = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var users = await _adminService.GetUsersAsync(page, pageSize, isSimulated);
        var totalCount = await _adminService.GetUserCountAsync(isSimulated);

        return Ok(new
        {
            data = users.Select(u => new
            {
                u.UniqueId,
                u.Name,
                u.Email,
                u.LifetimePoints,
                u.PreferredDifficulty,
                u.IsActive,
                u.IsSimulated,
                u.CreatedAt
            }),
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        });
    }

    [HttpPatch("users/{uniqueId}/status")]
    public async Task<IActionResult> SetUserStatus(string uniqueId, [FromBody] SetUserStatusRequest request)
    {
        var success = await _adminService.SetUserActiveStatusAsync(uniqueId, request.IsActive);
        if (!success)
            return NotFound(new { error = new { code = "USER_NOT_FOUND", message = "User not found" } });

        return Ok(new { data = new { uniqueId, isActive = request.IsActive } });
    }

    [HttpGet("users/{uniqueId}/analytics")]
    public async Task<IActionResult> GetPlayerAnalytics(string uniqueId)
    {
        var stats = await _adminService.GetPlayerAnalyticsAsync(uniqueId);
        if (stats == null)
            return NotFound(new { error = new { code = "STATS_NOT_FOUND", message = "Player stats not found" } });

        return Ok(new { data = stats });
    }
}

public class SetUserStatusRequest
{
    public bool IsActive { get; set; }
}
