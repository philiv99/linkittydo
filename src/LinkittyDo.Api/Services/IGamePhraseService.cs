using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Service interface for managing game phrases and phrase selection logic
/// </summary>
public interface IGamePhraseService
{
    Task<Phrase> GetPhraseForUserAsync(string? userId, int preferredDifficulty = 10);
    Task<Phrase?> GetPhraseByUniqueIdAsync(string uniqueId);
    Task<IEnumerable<GamePhrase>> GetAllPhrasesAsync();
    Task<int> GetPhraseCountAsync();
}
