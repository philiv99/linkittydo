using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Data;

/// <summary>
/// Repository interface for GamePhrase data access.
/// Manages the collection of all game phrases available for play.
/// </summary>
public interface IGamePhraseRepository
{
    /// <summary>
    /// Get all game phrases
    /// </summary>
    Task<IEnumerable<GamePhrase>> GetAllAsync();

    /// <summary>
    /// Get a game phrase by its unique ID
    /// </summary>
    Task<GamePhrase?> GetByIdAsync(string uniqueId);

    /// <summary>
    /// Get a game phrase by its text (case-insensitive)
    /// </summary>
    Task<GamePhrase?> GetByTextAsync(string text);

    /// <summary>
    /// Create a new game phrase
    /// </summary>
    Task<GamePhrase> CreateAsync(GamePhrase phrase);

    /// <summary>
    /// Delete a game phrase by its unique ID
    /// </summary>
    Task<bool> DeleteAsync(string uniqueId);

    /// <summary>
    /// Check if a phrase with the given text already exists (case-insensitive)
    /// </summary>
    Task<bool> ExistsByTextAsync(string text);

    /// <summary>
    /// Get the count of all phrases
    /// </summary>
    Task<int> GetCountAsync();
}
