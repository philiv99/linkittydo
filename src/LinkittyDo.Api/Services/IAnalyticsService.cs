using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IAnalyticsService
{
    Task RecomputePlayerStatsAsync(string userId);
    Task RecomputePhrasePlayStatsAsync(string phraseUniqueId);
    Task RecomputeClueEffectivenessAsync();
    Task<PlayerStats?> GetPlayerStatsAsync(string userId);
    Task<PhrasePlayStats?> GetPhrasePlayStatsAsync(string phraseUniqueId);
    Task<IReadOnlyList<ClueEffectiveness>> GetTopCluesAsync(string targetWord, int top = 5);
}
