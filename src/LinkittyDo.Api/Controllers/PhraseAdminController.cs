using LinkittyDo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/admin/phrases")]
[Authorize(Policy = "RequireAdmin")]
public class PhraseAdminController : ControllerBase
{
    private readonly IPhraseAdminService _phraseAdminService;
    private readonly IGamesManagerService _gamesManagerService;

    public PhraseAdminController(IPhraseAdminService phraseAdminService, IGamesManagerService gamesManagerService)
    {
        _phraseAdminService = phraseAdminService;
        _gamesManagerService = gamesManagerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPhrases([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var phrases = await _phraseAdminService.GetPhrasesAsync(page, pageSize, isActive);
        var totalCount = await _phraseAdminService.GetPhraseCountAsync(isActive);

        return Ok(new
        {
            data = phrases.Select(p => new
            {
                p.UniqueId,
                p.Text,
                p.WordCount,
                p.Difficulty,
                p.IsActive,
                p.GeneratedByLlm,
                p.CreatedAt
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

    [HttpPost]
    public async Task<IActionResult> CreatePhrase([FromBody] CreatePhraseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Phrase text is required" } });

        if (request.Text.Trim().Length < 3 || request.Text.Trim().Length > 500)
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Phrase text must be between 3 and 500 characters" } });

        try
        {
            var phrase = await _phraseAdminService.CreatePhraseAsync(request.Text, request.Difficulty);

            return Created($"/api/admin/phrases/{phrase.UniqueId}", new
            {
                data = new
                {
                    phrase.UniqueId,
                    phrase.Text,
                    phrase.WordCount,
                    phrase.Difficulty,
                    phrase.IsActive,
                    phrase.GeneratedByLlm,
                    phrase.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "PHRASE_EXISTS")
        {
            return Conflict(new { error = new { code = "PHRASE_EXISTS", message = "A phrase with this text already exists" } });
        }
    }

    [HttpPut("{uniqueId}")]
    public async Task<IActionResult> UpdatePhrase(string uniqueId, [FromBody] UpdatePhraseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Phrase text is required" } });

        if (request.Text.Trim().Length < 3 || request.Text.Trim().Length > 500)
            return BadRequest(new { error = new { code = "VALIDATION_ERROR", message = "Phrase text must be between 3 and 500 characters" } });

        var phrase = await _phraseAdminService.UpdatePhraseAsync(uniqueId, request.Text, request.Difficulty);
        if (phrase == null)
            return NotFound(new { error = new { code = "PHRASE_NOT_FOUND", message = "Phrase not found" } });

        return Ok(new
        {
            data = new
            {
                phrase.UniqueId,
                phrase.Text,
                phrase.WordCount,
                phrase.Difficulty,
                phrase.IsActive,
                phrase.GeneratedByLlm,
                phrase.CreatedAt
            }
        });
    }

    [HttpPatch("{uniqueId}/status")]
    public async Task<IActionResult> SetPhraseStatus(string uniqueId, [FromBody] SetPhraseStatusRequest request)
    {
        var success = await _phraseAdminService.SetPhraseActiveStatusAsync(uniqueId, request.IsActive);
        if (!success)
            return NotFound(new { error = new { code = "PHRASE_NOT_FOUND", message = "Phrase not found" } });

        return Ok(new { data = new { uniqueId, isActive = request.IsActive } });
    }

    [HttpGet("{uniqueId}/stats")]
    public async Task<IActionResult> GetPhraseStats(string uniqueId)
    {
        var stats = await _gamesManagerService.GetPhraseStatsAsync(uniqueId);
        if (stats == null)
            return NotFound(new { error = new { code = "STATS_NOT_FOUND", message = "Phrase stats not found" } });

        return Ok(new { data = stats });
    }
}

public class CreatePhraseRequest
{
    public string Text { get; set; } = string.Empty;
    public int Difficulty { get; set; }
}

public class UpdatePhraseRequest
{
    public string Text { get; set; } = string.Empty;
    public int Difficulty { get; set; }
}

public class SetPhraseStatusRequest
{
    public bool IsActive { get; set; }
}
