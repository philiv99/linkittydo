using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IUserService _userService;
    private readonly IGameRecordRepository _gameRecordRepository;

    public GameController(IGameService gameService, IUserService userService, IGameRecordRepository gameRecordRepository)
    {
        _gameService = gameService;
        _userService = userService;
        _gameRecordRepository = gameRecordRepository;
    }

    /// <summary>
    /// Start a new game session
    /// </summary>
    [HttpPost("start")]
    [EnableRateLimiting("game-start")]
    public async Task<ActionResult<ApiResponse<GameState>>> StartGame([FromBody] StartGameRequest? request = null)
    {
        var userId = request?.UserId;
        var difficulty = request?.Difficulty ?? 10;
        
        var session = await _gameService.StartNewGameAsync(userId, difficulty);
        var state = _gameService.GetGameState(session.SessionId);
        return Ok(new ApiResponse<GameState>(state, "Game started successfully"));
    }

    /// <summary>
    /// Get the current state of a game
    /// </summary>
    [HttpGet("{sessionId}")]
    public ActionResult<ApiResponse<GameState>> GetGame(Guid sessionId)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "GAME_NOT_FOUND",
                    Message = "Game session not found"
                }
            });
        }

        var state = _gameService.GetGameState(sessionId);
        return Ok(new ApiResponse<GameState>(state));
    }

    /// <summary>
    /// Submit a guess for a hidden word
    /// </summary>
    [HttpPost("{sessionId}/guess")]
    public async Task<ActionResult<ApiResponse<GuessResponse>>> SubmitGuess(Guid sessionId, [FromBody] GuessRequest request)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "GAME_NOT_FOUND",
                    Message = "Game session not found"
                }
            });
        }

        var response = await _gameService.SubmitGuessAsync(sessionId, request);
        
        return Ok(new ApiResponse<GuessResponse>(response));
    }

    /// <summary>
    /// Give up and reveal the complete phrase
    /// </summary>
    [HttpPost("{sessionId}/give-up")]
    public async Task<ActionResult<ApiResponse<GameState>>> GiveUp(Guid sessionId)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "GAME_NOT_FOUND",
                    Message = "Game session not found"
                }
            });
        }

        var state = await _gameService.GiveUpAsync(sessionId);
        
        return Ok(new ApiResponse<GameState>(state));
    }
    
    /// <summary>
    /// Get game record for a completed game
    /// </summary>
    [HttpGet("{sessionId}/record")]
    public ActionResult<ApiResponse<GameRecord>> GetGameRecord(Guid sessionId)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "GAME_NOT_FOUND",
                    Message = "Game session not found"
                }
            });
        }

        if (session.IsGuestSession || session.GameRecord == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "GAME_NOT_FOUND",
                    Message = "No game record available for guest sessions"
                }
            });
        }

        return Ok(new ApiResponse<GameRecord>(session.GameRecord));
    }

    /// <summary>
    /// Get a completed game record with all events from the database
    /// </summary>
    [HttpGet("detail/{gameId}")]
    public async Task<ActionResult<ApiResponse<GameRecord>>> GetGameDetail(string gameId)
    {
        var record = await _gameRecordRepository.GetByGameIdWithEventsAsync(gameId);
        if (record == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "GAME_NOT_FOUND",
                    Message = "Game record not found"
                }
            });
        }

        return Ok(new ApiResponse<GameRecord>(record, "Game record retrieved successfully"));
    }
}
