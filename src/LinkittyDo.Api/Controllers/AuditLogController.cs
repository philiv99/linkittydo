using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/admin/audit-log")]
[Authorize(Policy = "RequireAdmin")]
public class AuditLogController : ControllerBase
{
    private readonly LinkittyDoDbContext _db;

    public AuditLogController(LinkittyDoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = _db.AuditLog.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var total = await query.CountAsync();
        var entries = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = entries,
            pagination = new
            {
                page,
                pageSize,
                totalItems = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        });
    }

    [HttpGet("actions")]
    public async Task<IActionResult> GetDistinctActions()
    {
        var actions = await _db.AuditLog
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return Ok(actions);
    }
}
