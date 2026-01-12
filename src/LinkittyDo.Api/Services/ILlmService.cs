namespace LinkittyDo.Api.Services;

/// <summary>
/// Response from an LLM completion request
/// </summary>
public class LlmResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

/// <summary>
/// Interface for LLM service operations
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Sends a prompt to the LLM and returns the completion response
    /// </summary>
    /// <param name="prompt">The user prompt to send</param>
    /// <param name="systemPrompt">Optional system prompt for context</param>
    /// <returns>The LLM response with content and token usage</returns>
    Task<LlmResponse> GetCompletionAsync(string prompt, string? systemPrompt = null);

    /// <summary>
    /// Generates multiple unique phrases for the word guessing game
    /// </summary>
    /// <param name="count">Number of phrases to generate (default 10)</param>
    /// <returns>List of unique phrase strings</returns>
    Task<List<string>> GeneratePhrasesAsync(int count = 10);
}
