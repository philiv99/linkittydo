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
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isSimulated = null, [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var users = await _adminService.GetUsersAsync(page, pageSize, isSimulated, search);
        var totalCount = await _adminService.GetUserCountAsync(isSimulated, search);

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

    [HttpGet("users/{uniqueId}/roles")]
    public async Task<IActionResult> GetUserRoles(string uniqueId)
    {
        var roles = await _adminService.GetUserRolesAsync(uniqueId);
        return Ok(new { data = new { uniqueId, roles } });
    }

    [HttpPost("users/{uniqueId}/roles")]
    public async Task<IActionResult> AssignRole(string uniqueId, [FromBody] RoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Role name is required" } });

        var success = await _adminService.AssignRoleAsync(uniqueId, request.RoleName);
        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User or role not found" } });

        var roles = await _adminService.GetUserRolesAsync(uniqueId);
        return Ok(new { data = new { uniqueId, roles } });
    }

    [HttpDelete("users/{uniqueId}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(string uniqueId, string roleName)
    {
        var success = await _adminService.RemoveRoleAsync(uniqueId, roleName);
        if (!success)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User role assignment not found" } });

        var roles = await _adminService.GetUserRolesAsync(uniqueId);
        return Ok(new { data = new { uniqueId, roles } });
    }

    [HttpDelete("users/{uniqueId}")]
    public async Task<IActionResult> HardDeleteUser(string uniqueId)
    {
        var success = await _adminService.HardDeleteUserAsync(uniqueId);
        if (!success)
            return NotFound(new { error = new { code = "USER_NOT_FOUND", message = "User not found" } });

        return NoContent();
    }
}

public class SetUserStatusRequest
{
    public bool IsActive { get; set; }
}

public class RoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}
