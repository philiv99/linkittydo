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

    public async Task<IReadOnlyList<User>> GetUsersAsync(int page = 1, int pageSize = 20, bool? isSimulated = null, string? search = null)
    {
        var query = _context.Users.AsQueryable();
        if (isSimulated.HasValue)
            query = query.Where(u => u.IsSimulated == isSimulated.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUserCountAsync(bool? isSimulated = null, string? search = null)
    {
        var query = _context.Users.AsQueryable();
        if (isSimulated.HasValue)
            query = query.Where(u => u.IsSimulated == isSimulated.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }
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

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();
    }

    public async Task<bool> AssignRoleAsync(string userId, string roleName)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return false;

        var exists = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (exists) return true;

        _context.UserRoles.Add(new Models.UserRole
        {
            UserId = userId,
            RoleId = role.Id,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveRoleAsync(string userId, string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return false;

        var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (userRole == null) return false;

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HardDeleteUserAsync(string uniqueId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UniqueId == uniqueId);
        if (user == null) return false;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // 1. Delete GameEvents for all the user's games
        var gameIds = await _context.GameRecords
            .Where(g => g.UserId == uniqueId)
            .Select(g => g.GameId)
            .ToListAsync();

        if (gameIds.Count > 0)
        {
            var events = await _context.GameEvents
                .Where(e => gameIds.Contains(e.GameId))
                .ToListAsync();
            _context.GameEvents.RemoveRange(events);
        }

        // 2. Delete GameRecords
        var gameRecords = await _context.GameRecords
            .Where(g => g.UserId == uniqueId)
            .ToListAsync();
        _context.GameRecords.RemoveRange(gameRecords);

        // 3. Delete GameSessions
        var gameSessions = await _context.GameSessions
            .Where(gs => gs.UserId == uniqueId)
            .ToListAsync();
        _context.GameSessions.RemoveRange(gameSessions);

        // 4. Delete UserRoles
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == uniqueId)
            .ToListAsync();
        _context.UserRoles.RemoveRange(userRoles);

        // 5. Delete PlayerStats
        var playerStats = await _context.PlayerStats.FindAsync(uniqueId);
        if (playerStats != null)
            _context.PlayerStats.Remove(playerStats);

        // 6. Delete AuditLog entries for this user
        var auditEntries = await _context.AuditLog
            .Where(a => a.UserId == uniqueId)
            .ToListAsync();
        _context.AuditLog.RemoveRange(auditEntries);

        // 7. Delete the User record
        _context.Users.Remove(user);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }
}
