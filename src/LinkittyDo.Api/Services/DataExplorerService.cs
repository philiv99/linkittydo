using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public class DataExplorerService : IDataExplorerService
{
    private readonly LinkittyDoDbContext _context;

    public DataExplorerService(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<SimulationSummary> GetSimulationSummaryAsync()
    {
        var simUsers = await _context.Users.Where(u => u.IsSimulated).CountAsync();
        var simGames = await _context.GameRecords
            .Where(g => g.IsSimulated && g.Result != GameResult.InProgress)
            .ToListAsync();

        var solved = simGames.Count(g => g.Result == GameResult.Solved);
        var gaveUp = simGames.Count(g => g.Result == GameResult.GaveUp);
        var total = simGames.Count;

        // Group by profile using user name pattern (SimBot_{ProfileName}_{random})
        var profileCounts = new Dictionary<string, int>();
        var simUserNames = await _context.Users
            .Where(u => u.IsSimulated)
            .Select(u => u.Name)
            .ToListAsync();

        foreach (var name in simUserNames)
        {
            var parts = name.Split('_');
            var profileName = parts.Length >= 2 ? parts[1] : "Unknown";
            profileCounts.TryGetValue(profileName, out var count);
            profileCounts[profileName] = count + 1;
        }

        return new SimulationSummary
        {
            TotalSimUsers = simUsers,
            TotalSimGames = total,
            SimSolved = solved,
            SimGaveUp = gaveUp,
            SimSolveRate = total > 0 ? (double)solved / total : 0,
            SimAvgScore = simGames.Count > 0 ? simGames.Average(g => g.Score) : 0,
            GamesByProfile = profileCounts
        };
    }

    public async Task<PlayerDetail?> GetPlayerDetailAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var stats = await _context.PlayerStats.FindAsync(userId);
        var recentGames = await _context.GameRecords
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.PlayedAt)
            .Take(10)
            .ToListAsync();

        return new PlayerDetail
        {
            User = user,
            Stats = stats,
            RecentGames = recentGames
        };
    }

    public async Task<DataSummary> GetDataSummaryAsync()
    {
        var games = await _context.GameRecords.ToListAsync();
        var eventCount = await _context.GameEvents.CountAsync();

        return new DataSummary
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalPhrases = await _context.GamePhrases.CountAsync(),
            TotalGames = games.Count,
            TotalEvents = eventCount,
            TotalSimUsers = await _context.Users.CountAsync(u => u.IsSimulated),
            TotalSimGames = games.Count(g => g.IsSimulated),
            EstimatedStorageSizeBytes = (games.Count * 500L) + (eventCount * 200L), // rough estimate
            OldestGame = games.Count > 0 ? games.Min(g => g.PlayedAt) : DateTime.MinValue,
            NewestGame = games.Count > 0 ? games.Max(g => g.PlayedAt) : DateTime.MinValue
        };
    }
}
