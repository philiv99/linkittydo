using Microsoft.AspNetCore.Mvc;
using LinkittyDo.Api.Services;

namespace LinkittyDo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly ILogger<LlmController> _logger;

    public LlmController(ILlmService llmService, ILogger<LlmController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint for LLM completions
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<LlmTestResponse>> TestCompletion([FromBody] LlmTestRequest request)
    {
        _logger.LogInformation("LLM Test endpoint called with prompt: {Prompt}", request.Prompt);

        try
        {
            var response = await _llmService.GetCompletionAsync(request.Prompt, request.SystemPrompt);
            
            return Ok(new LlmTestResponse
            {
                Success = true,
                Content = response.Content,
                Model = response.Model,
                PromptTokens = response.PromptTokens,
                CompletionTokens = response.CompletionTokens,
                TotalTokens = response.TotalTokens
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LLM test endpoint");
            return StatusCode(500, new LlmTestResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
}

public class LlmTestRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
}

public class LlmTestResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? Model { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public string? Error { get; set; }
}
