using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Service interface for managing game phrases and phrase selection logic
/// </summary>
public interface IGamePhraseService
{
    /// <summary>
    /// Gets a phrase for the specified user to play.
    /// The phrase selection follows this logic:
    /// 1. Get all phrases the user has already played from their game history
    /// 2. Find phrases in the manager that the user hasn't played yet
    /// 3. If no unplayed phrases exist, generate a new phrase using LLM
    /// 4. Add new LLM-generated phrases to the manager (if unique)
    /// 5. Return a phrase the user can play
    /// </summary>
    /// <param name="userId">The user's unique ID (null for guest users)</param>
    /// <returns>A Phrase object ready for gameplay</returns>
    Task<Phrase> GetPhraseForUserAsync(string? userId);

    /// <summary>
    /// Gets all phrases in the manager
    /// </summary>
    Task<IEnumerable<GamePhrase>> GetAllPhrasesAsync();

    /// <summary>
    /// Gets the count of phrases in the manager
    /// </summary>
    Task<int> GetPhraseCountAsync();
}
