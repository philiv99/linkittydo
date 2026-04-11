using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public class GameSearchResult
{
    public IReadOnlyList<GameRecord> Games { get; set; } = Array.Empty<GameRecord>();
    public Dictionary<string, string> PlayerNames { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public interface IGamesManagerService
{
    Task<GameSearchResult> SearchGamesAsync(int page = 1, int pageSize = 20, string? userId = null, GameResult? result = null, bool? isSimulated = null);
    Task<GameRecord?> GetGameDetailAsync(string gameId);
    Task<IReadOnlyList<GameEvent>> GetGameEventsAsync(string gameId);
    Task<PhrasePlayStats?> GetPhraseStatsAsync(string phraseUniqueId);
}
