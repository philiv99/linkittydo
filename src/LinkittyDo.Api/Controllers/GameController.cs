using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IUserService _userService;

    public GameController(IGameService gameService, IUserService userService)
    {
        _gameService = gameService;
        _userService = userService;
    }

    /// <summary>
    /// Start a new game session
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<GameState>> StartGame([FromBody] StartGameRequest? request = null)
    {
        var userId = request?.UserId;
        var difficulty = request?.Difficulty ?? 10;
        
        var session = await _gameService.StartNewGameAsync(userId, difficulty);
        var state = _gameService.GetGameState(session.SessionId);
        return Ok(state);
    }

    /// <summary>
    /// Get the current state of a game
    /// </summary>
    [HttpGet("{sessionId}")]
    public ActionResult<GameState> GetGame(Guid sessionId)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Game session not found" });
        }

        var state = _gameService.GetGameState(sessionId);
        return Ok(state);
    }

    /// <summary>
    /// Submit a guess for a hidden word
    /// </summary>
    [HttpPost("{sessionId}/guess")]
    public async Task<ActionResult<GuessResponse>> SubmitGuess(Guid sessionId, [FromBody] GuessRequest request)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Game session not found" });
        }

        var response = _gameService.SubmitGuess(sessionId, request);
        
        // If phrase is complete and this is not a guest, save the game record
        if (response.IsPhraseComplete && !session.IsGuestSession && session.GameRecord != null)
        {
            await _userService.AddGameRecordAsync(session.UserId!, session.GameRecord);
        }
        
        return Ok(response);
    }

    /// <summary>
    /// Give up and reveal the complete phrase
    /// </summary>
    [HttpPost("{sessionId}/give-up")]
    public async Task<ActionResult<GameState>> GiveUp(Guid sessionId)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Game session not found" });
        }

        var state = _gameService.GiveUp(sessionId);
        
        // Save the game record for non-guest users
        if (!session.IsGuestSession && session.GameRecord != null)
        {
            await _userService.AddGameRecordAsync(session.UserId!, session.GameRecord);
        }
        
        return Ok(state);
    }
    
    /// <summary>
    /// Get game record for a completed game
    /// </summary>
    [HttpGet("{sessionId}/record")]
    public ActionResult<GameRecord> GetGameRecord(Guid sessionId)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Game session not found" });
        }

        if (session.IsGuestSession || session.GameRecord == null)
        {
            return NotFound(new { message = "No game record available for guest sessions" });
        }

        return Ok(session.GameRecord);
    }
}
