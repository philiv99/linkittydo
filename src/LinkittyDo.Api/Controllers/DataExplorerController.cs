using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/admin/data")]
[Authorize(Policy = "RequireAdmin")]
public class DataExplorerController : ControllerBase
{
    private readonly IDataExplorerService _dataExplorer;

    public DataExplorerController(IDataExplorerService dataExplorer)
    {
        _dataExplorer = dataExplorer;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetDataSummary()
    {
        var summary = await _dataExplorer.GetDataSummaryAsync();
        return Ok(new { data = summary });
    }

    [HttpGet("simulation-summary")]
    public async Task<IActionResult> GetSimulationSummary()
    {
        var summary = await _dataExplorer.GetSimulationSummaryAsync();
        return Ok(new { data = summary });
    }

    [HttpGet("player/{userId}")]
    public async Task<IActionResult> GetPlayerDetail(string userId)
    {
        var detail = await _dataExplorer.GetPlayerDetailAsync(userId);
        if (detail == null)
            return NotFound(new { error = new { code = "USER_NOT_FOUND", message = "User not found" } });

        return Ok(new
        {
            data = new
            {
                user = new
                {
                    detail.User!.UniqueId,
                    detail.User.Name,
                    detail.User.Email,
                    detail.User.LifetimePoints,
                    detail.User.IsSimulated,
                    detail.User.IsActive,
                    detail.User.CreatedAt
                },
                stats = detail.Stats,
                recentGames = detail.RecentGames.Select(g => new
                {
                    g.GameId,
                    g.PhraseText,
                    Result = g.Result.ToString(),
                    g.Score,
                    g.PlayedAt
                })
            }
        });
    }
}
