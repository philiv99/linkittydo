using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/daily-challenge")]
public class DailyChallengeController : ControllerBase
{
    private readonly IDailyChallengeService _dailyChallengeService;
    private readonly IGameService _gameService;

    public DailyChallengeController(IDailyChallengeService dailyChallengeService, IGameService gameService)
    {
        _dailyChallengeService = dailyChallengeService;
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DailyChallengeResponse>>> GetTodaysChallenge([FromQuery] string? userId = null)
    {
        var status = await _dailyChallengeService.GetChallengeStatusAsync(userId);
        return Ok(new ApiResponse<DailyChallengeResponse>(status, "Daily challenge retrieved"));
    }

    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse<GameState>>> StartDailyChallenge([FromBody] StartGameRequest? request = null)
    {
        var userId = request?.UserId;
        var difficulty = request?.Difficulty ?? 10;

        // Check if user already played today
        if (!string.IsNullOrEmpty(userId))
        {
            var status = await _dailyChallengeService.GetChallengeStatusAsync(userId);
            if (status.AlreadyPlayed)
            {
                return Conflict(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "ALREADY_PLAYED",
                        Message = "You have already completed today's daily challenge"
                    }
                });
            }
        }

        var session = await _gameService.StartDailyChallengeAsync(userId, difficulty);
        var state = _gameService.GetGameState(session.SessionId);
        return Ok(new ApiResponse<GameState>(state, "Daily challenge started"));
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DailyChallengeLeaderboardEntry>>>> GetLeaderboard(
        [FromQuery] DateTime? date = null, [FromQuery] int top = 10)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;
        var entries = await _dailyChallengeService.GetDailyLeaderboardAsync(targetDate, top);
        return Ok(new ApiResponse<IReadOnlyList<DailyChallengeLeaderboardEntry>>(entries, "Daily leaderboard retrieved"));
    }
}
