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

    public async Task<List<string>> GeneratePhrasesAsync(int count = 10)
    {
        _logger.LogInformation("Generating {Count} unique phrases via LLM", count);

        var systemPrompt = @"You are a phrase generator for a word guessing game. 
Your task is to generate well-known phrases, idioms, proverbs, or common sayings.
Rules:
- Each phrase must be 7 words or less
- Use common, well-known phrases that most English speakers would recognize
- Do not include quotes around phrases
- Each phrase must be on its own line
- Do not number the phrases
- Do not include explanations or context
- All phrases must be UNIQUE - no duplicates";

        var userPrompt = $@"Generate exactly {count} unique, well-known English phrases, idioms, proverbs, or common sayings.
Each phrase must be 7 words or less.
Put each phrase on its own line with no numbering, no quotes, and no additional text.

Examples of good phrases:
- Actions speak louder than words
- Better late than never
- Knowledge is power
- Practice makes perfect
- Time is money
- A penny saved is a penny earned

Return only the {count} phrases, one per line.";

        try
        {
            var response = await GetCompletionAsync(userPrompt, systemPrompt);
            
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("LLM returned empty content for batch phrase generation");
                return new List<string>();
            }

            // Parse the response - split by newlines and clean up each phrase
            var phrases = response.Content
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line
                    .Trim()
                    .TrimStart('-', '*', '•', ' ', '\t')  // Remove list markers
                    .Trim()
                    .Trim('"', '\'', '.', '!', '?')       // Remove quotes and trailing punctuation
                    .Trim())
                .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
                .Where(phrase => 
                {
                    // Filter out numbered lines like "1." or "1)"
                    if (phrase.Length > 0 && char.IsDigit(phrase[0]))
                    {
                        var firstNonDigit = phrase.SkipWhile(char.IsDigit).FirstOrDefault();
                        if (firstNonDigit == '.' || firstNonDigit == ')' || firstNonDigit == ':')
                        {
                            // Remove the numbering and keep the rest
                            var indexOfSeparator = phrase.IndexOfAny(new[] { '.', ')', ':' });
                            if (indexOfSeparator >= 0 && indexOfSeparator < phrase.Length - 1)
                            {
                                return false; // We'll handle this in the Select below
                            }
                        }
                    }
                    return true;
                })
                .Select(phrase =>
                {
                    // Also handle numbered lines by extracting just the phrase part
                    if (phrase.Length > 0 && char.IsDigit(phrase[0]))
                    {
                        var indexOfSeparator = phrase.IndexOfAny(new[] { '.', ')', ':' });
                        if (indexOfSeparator >= 0 && indexOfSeparator < phrase.Length - 1)
                        {
                            return phrase.Substring(indexOfSeparator + 1).Trim();
                        }
                    }
                    return phrase;
                })
                .Where(phrase =>
                {
                    var wordCount = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    if (wordCount > 7)
                    {
                        _logger.LogDebug("Filtering out phrase with {WordCount} words: {Phrase}", wordCount, phrase);
                        return false;
                    }
                    if (wordCount < 2)
                    {
                        _logger.LogDebug("Filtering out phrase with too few words: {Phrase}", phrase);
                        return false;
                    }
                    return true;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation("LLM generated {Count} valid unique phrases", phrases.Count);
            
            foreach (var phrase in phrases)
            {
                _logger.LogDebug("Generated phrase: {Phrase}", phrase);
            }

            return phrases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch phrases from LLM");
            return new List<string>();
        }
    }
}
