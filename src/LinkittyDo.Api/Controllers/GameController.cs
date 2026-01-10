using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    /// <summary>
    /// Start a new game session
    /// </summary>
    [HttpPost("start")]
    public ActionResult<GameState> StartGame()
    {
        var session = _gameService.StartNewGame();
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
    public ActionResult<GuessResponse> SubmitGuess(Guid sessionId, [FromBody] GuessRequest request)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Game session not found" });
        }

        var response = _gameService.SubmitGuess(sessionId, request);
        return Ok(response);
    }
}
