using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinkittyDo.Api.Services;

/// <summary>
/// OpenAI LLM service implementation
/// </summary>
public class OpenAiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiLlmService> _logger;
    private readonly string _model;
    private readonly int _maxTokens;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    public OpenAiLlmService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiLlmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Get API key from environment variable first, then fall back to config
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                     ?? configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured. Set OPENAI_API_KEY environment variable or OpenAI:ApiKey in appsettings.json");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);
        }
        
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _maxTokens = configuration.GetValue<int>("OpenAI:MaxTokens", 1000);
        
        _logger.LogInformation("OpenAI LLM Service initialized with model: {Model}, maxTokens: {MaxTokens}", 
            _model, _maxTokens);
    }

    public async Task<LlmResponse> GetCompletionAsync(string prompt, string? systemPrompt = null)
    {
        _logger.LogInformation("=== LLM Request ===");
        _logger.LogInformation("Model: {Model}", _model);
        _logger.LogInformation("System Prompt: {SystemPrompt}", systemPrompt ?? "(none)");
        _logger.LogInformation("User Prompt: {Prompt}", prompt);

        var messages = new List<OpenAiMessage>();
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new OpenAiMessage { Role = "system", Content = systemPrompt });
        }
        
        messages.Add(new OpenAiMessage { Role = "user", Content = prompt });

        var request = new OpenAiRequest
        {
            Model = _model,
            Messages = messages,
            MaxTokens = _maxTokens
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
        _logger.LogDebug("Request JSON: {Json}", jsonContent);

        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(OpenAiApiUrl, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OpenAI API error: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode} - {responseContent}");
            }

            var openAiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseContent, jsonOptions);

            if (openAiResponse?.Choices == null || openAiResponse.Choices.Count == 0)
            {
                _logger.LogError("No choices returned from OpenAI API");
                throw new InvalidOperationException("No response from OpenAI API");
            }

            var result = new LlmResponse
            {
                Content = openAiResponse.Choices[0].Message?.Content ?? string.Empty,
                Model = openAiResponse.Model ?? _model,
                PromptTokens = openAiResponse.Usage?.PromptTokens ?? 0,
                CompletionTokens = openAiResponse.Usage?.CompletionTokens ?? 0,
                TotalTokens = openAiResponse.Usage?.TotalTokens ?? 0
            };

            _logger.LogInformation("=== LLM Response ===");
            _logger.LogInformation("Response Content: {Content}", result.Content);
            _logger.LogInformation("Model Used: {Model}", result.Model);
            _logger.LogInformation("Token Usage - Prompt: {PromptTokens}, Completion: {CompletionTokens}, Total: {TotalTokens}",
                result.PromptTokens, result.CompletionTokens, result.TotalTokens);

            return result;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw;
        }
    }

    #region OpenAI API Models

    private class OpenAiRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<OpenAiMessage> Messages { get; set; } = new();
        public int MaxTokens { get; set; }
    }

    private class OpenAiMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAiResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long Created { get; set; }
        public string? Model { get; set; }
        public List<OpenAiChoice>? Choices { get; set; }
        public OpenAiUsage? Usage { get; set; }
    }

    private class OpenAiChoice
    {
        public int Index { get; set; }
        public OpenAiMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class OpenAiUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    #endregion
}
