using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public class NoOpAnalyticsService : IAnalyticsService
{
    public Task RecomputePlayerStatsAsync(string userId) => Task.CompletedTask;
    public Task RecomputePhrasePlayStatsAsync(string phraseUniqueId) => Task.CompletedTask;
    public Task RecomputeClueEffectivenessAsync() => Task.CompletedTask;
    public Task RecomputeClueEffectivenessForGameAsync(string gameId, IEnumerable<GameEvent> events) => Task.CompletedTask;
    public Task<PlayerStats?> GetPlayerStatsAsync(string userId) => Task.FromResult<PlayerStats?>(null);
    public Task<PhrasePlayStats?> GetPhrasePlayStatsAsync(string phraseUniqueId) => Task.FromResult<PhrasePlayStats?>(null);
    public Task<IReadOnlyList<ClueEffectiveness>> GetTopCluesAsync(string targetWord, int top = 5)
        => Task.FromResult<IReadOnlyList<ClueEffectiveness>>(Array.Empty<ClueEffectiveness>());
}
