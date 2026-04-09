using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public class GamesManagerService : IGamesManagerService
{
    private readonly LinkittyDoDbContext _context;

    public GamesManagerService(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<GameSearchResult> SearchGamesAsync(int page = 1, int pageSize = 20, string? userId = null, GameResult? result = null, bool? isSimulated = null)
    {
        var query = _context.GameRecords.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(g => g.UserId == userId);
        if (result.HasValue)
            query = query.Where(g => g.Result == result.Value);
        if (isSimulated.HasValue)
            query = query.Where(g => g.IsSimulated == isSimulated.Value);

        var totalCount = await query.CountAsync();
        var games = await query
            .OrderByDescending(g => g.PlayedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new GameSearchResult
        {
            Games = games,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<GameRecord?> GetGameDetailAsync(string gameId)
    {
        return await _context.GameRecords.FindAsync(gameId);
    }

    public async Task<IReadOnlyList<GameEvent>> GetGameEventsAsync(string gameId)
    {
        return await _context.GameEvents
            .Where(e => e.GameId == gameId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync();
    }

    public async Task<PhrasePlayStats?> GetPhraseStatsAsync(string phraseUniqueId)
    {
        return await _context.PhrasePlayStats.FindAsync(phraseUniqueId);
    }
}
