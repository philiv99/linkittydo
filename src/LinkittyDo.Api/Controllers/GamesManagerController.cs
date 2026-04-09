using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/admin/games")]
[Authorize(Policy = "RequireAdmin")]
public class GamesManagerController : ControllerBase
{
    private readonly IGamesManagerService _gamesManager;

    public GamesManagerController(IGamesManagerService gamesManager)
    {
        _gamesManager = gamesManager;
    }

    [HttpGet]
    public async Task<IActionResult> SearchGames(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? userId = null,
        [FromQuery] GameResult? result = null,
        [FromQuery] bool? isSimulated = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var searchResult = await _gamesManager.SearchGamesAsync(page, pageSize, userId, result, isSimulated);

        return Ok(new
        {
            data = searchResult.Games.Select(g => new
            {
                g.GameId,
                g.UserId,
                g.PhraseText,
                g.Difficulty,
                Result = g.Result.ToString(),
                g.Score,
                g.IsSimulated,
                g.PlayedAt,
                g.CompletedAt
            }),
            pagination = new
            {
                page = searchResult.Page,
                pageSize = searchResult.PageSize,
                totalCount = searchResult.TotalCount,
                totalPages = (int)Math.Ceiling((double)searchResult.TotalCount / searchResult.PageSize)
            }
        });
    }

    [HttpGet("{gameId}")]
    public async Task<IActionResult> GetGameDetail(string gameId)
    {
        var game = await _gamesManager.GetGameDetailAsync(gameId);
        if (game == null)
            return NotFound(new { error = new { code = "GAME_NOT_FOUND", message = "Game not found" } });

        var events = await _gamesManager.GetGameEventsAsync(gameId);

        return Ok(new
        {
            data = new
            {
                game.GameId,
                game.UserId,
                game.PhraseText,
                game.Difficulty,
                Result = game.Result.ToString(),
                game.Score,
                game.IsSimulated,
                game.PlayedAt,
                game.CompletedAt,
                EventCount = events.Count,
                Events = events.Select(e => new
                {
                    e.Id,
                    e.EventType,
                    e.SequenceNumber,
                    e.Timestamp
                })
            }
        });
    }

    [HttpGet("phrase-stats/{phraseUniqueId}")]
    public async Task<IActionResult> GetPhraseStats(string phraseUniqueId)
    {
        var stats = await _gamesManager.GetPhraseStatsAsync(phraseUniqueId);
        if (stats == null)
            return NotFound(new { error = new { code = "STATS_NOT_FOUND", message = "Phrase stats not found" } });

        return Ok(new { data = stats });
    }
}
