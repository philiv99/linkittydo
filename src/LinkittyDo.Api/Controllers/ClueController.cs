using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClueController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IClueService _clueService;

    public ClueController(IGameService gameService, IClueService clueService)
    {
        _gameService = gameService;
        _clueService = clueService;
    }

    /// <summary>
    /// Get a clue URL for a hidden word
    /// </summary>
    [HttpGet("{sessionId}/{wordIndex}")]
    public async Task<ActionResult<ClueResponse>> GetClue(Guid sessionId, int wordIndex)
    {
        var session = _gameService.GetGame(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Game session not found" });
        }

        var word = session.Phrase.Words.FirstOrDefault(w => w.Index == wordIndex);
        if (word == null)
        {
            return NotFound(new { message = "Word not found" });
        }

        if (!word.IsHidden)
        {
            return BadRequest(new { message = "Word is not hidden, no clue needed" });
        }

        var clue = await _clueService.GetClueAsync(session, wordIndex);
        return Ok(clue);
    }
}
