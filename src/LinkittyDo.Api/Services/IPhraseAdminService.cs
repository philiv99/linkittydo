using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IPhraseAdminService
{
    Task<IReadOnlyList<GamePhrase>> GetPhrasesAsync(int page = 1, int pageSize = 20, bool? isActive = null);
    Task<int> GetPhraseCountAsync(bool? isActive = null);
    Task<GamePhrase> CreatePhraseAsync(string text, int difficulty = 0);
    Task<GamePhrase?> UpdatePhraseAsync(string uniqueId, string text, int difficulty);
    Task<bool> SetPhraseActiveStatusAsync(string uniqueId, bool isActive);
}
