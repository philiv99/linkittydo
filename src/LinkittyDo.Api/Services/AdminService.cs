using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public class AdminService : IAdminService
{
    private readonly LinkittyDoDbContext _context;

    public AdminService(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var users = await _context.Users.ToListAsync();
        var games = await _context.GameRecords
            .Where(g => g.Result != GameResult.InProgress)
            .ToListAsync();

        var totalCompleted = games.Count;
        var solved = games.Count(g => g.Result == GameResult.Solved);
        var gaveUp = games.Count(g => g.Result == GameResult.GaveUp);

        return new DashboardStats
        {
            TotalUsers = users.Count(u => !u.IsSimulated),
            TotalGamesPlayed = games.Count(g => !g.IsSimulated),
            TotalGamesSolved = games.Count(g => !g.IsSimulated && g.Result == GameResult.Solved),
            TotalGamesGaveUp = games.Count(g => !g.IsSimulated && g.Result == GameResult.GaveUp),
            TotalPhrases = await _context.GamePhrases.CountAsync(),
            ActivePhrases = await _context.GamePhrases.CountAsync(p => p.IsActive),
            SimulatedUsers = users.Count(u => u.IsSimulated),
            SimulatedGames = games.Count(g => g.IsSimulated),
            OverallSolveRate = totalCompleted > 0 ? (double)solved / totalCompleted : 0,
            AvgScore = games.Count > 0 ? games.Average(g => g.Score) : 0,
            GamesPlayedToday = games.Count(g => g.PlayedAt.Date == today),
            NewUsersToday = users.Count(u => u.CreatedAt.Date == today),
            ComputedAt = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<User>> GetUsersAsync(int page = 1, int pageSize = 20, bool? isSimulated = null)
    {
        var query = _context.Users.AsQueryable();
        if (isSimulated.HasValue)
            query = query.Where(u => u.IsSimulated == isSimulated.Value);

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUserCountAsync(bool? isSimulated = null)
    {
        var query = _context.Users.AsQueryable();
        if (isSimulated.HasValue)
            query = query.Where(u => u.IsSimulated == isSimulated.Value);
        return await query.CountAsync();
    }

    public async Task<bool> SetUserActiveStatusAsync(string uniqueId, bool isActive)
    {
        var user = await _context.Users.FindAsync(uniqueId);
        if (user == null) return false;

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PlayerStats?> GetPlayerAnalyticsAsync(string userId)
    {
        return await _context.PlayerStats.FindAsync(userId);
    }
}
