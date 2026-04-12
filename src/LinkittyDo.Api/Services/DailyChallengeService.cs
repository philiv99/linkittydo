using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public class DailyChallengeService : IDailyChallengeService
{
    private readonly LinkittyDoDbContext _dbContext;
    private readonly ILogger<DailyChallengeService> _logger;

    public DailyChallengeService(LinkittyDoDbContext dbContext, ILogger<DailyChallengeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DailyChallenge> GetOrCreateTodaysChallengeAsync()
    {
        var today = DateTime.UtcNow.Date;
        var existing = await _dbContext.DailyChallenges
            .FirstOrDefaultAsync(dc => dc.Date == today);

        if (existing != null)
            return existing;

        // Select a phrase that has not been used as a daily challenge recently
        var recentPhraseIds = await _dbContext.DailyChallenges
            .OrderByDescending(dc => dc.Date)
            .Take(30)
            .Select(dc => dc.PhraseUniqueId)
            .ToListAsync();

        var phrase = await _dbContext.GamePhrases
            .Where(p => p.IsActive && !recentPhraseIds.Contains(p.UniqueId))
            .OrderBy(p => p.Difficulty)
            .Skip(new Random().Next(0, Math.Max(1, await _dbContext.GamePhrases
                .CountAsync(p => p.IsActive && !recentPhraseIds.Contains(p.UniqueId)) / 3)))
            .FirstOrDefaultAsync();

        if (phrase == null)
        {
            // Fallback: just pick any active phrase
            phrase = await _dbContext.GamePhrases
                .Where(p => p.IsActive)
                .OrderBy(p => Guid.NewGuid())
                .FirstOrDefaultAsync();
        }

        if (phrase == null)
            throw new InvalidOperationException("No phrases available for daily challenge");

        var challenge = new DailyChallenge
        {
            Date = today,
            PhraseUniqueId = phrase.UniqueId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.DailyChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created daily challenge for {Date} with phrase {PhraseId}", today, phrase.UniqueId);
        return challenge;
    }

    public async Task<DailyChallengeResponse> GetChallengeStatusAsync(string? userId)
    {
        var challenge = await GetOrCreateTodaysChallengeAsync();
        var response = new DailyChallengeResponse
        {
            Date = challenge.Date,
            PhraseUniqueId = challenge.PhraseUniqueId,
            AlreadyPlayed = false
        };

        if (!string.IsNullOrEmpty(userId))
        {
            var result = await _dbContext.DailyChallengeResults
                .FirstOrDefaultAsync(r => r.ChallengeDate == challenge.Date && r.UserId == userId);

            if (result != null)
            {
                response.AlreadyPlayed = true;
                response.PreviousResult = new DailyChallengeResultResponse
                {
                    Score = result.Score,
                    Result = result.Result.ToString(),
                    CompletedAt = result.CompletedAt
                };
            }
        }

        return response;
    }

    public async Task RecordChallengeResultAsync(string userId, string gameId, int score, GameResult result)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await _dbContext.DailyChallengeResults
            .FirstOrDefaultAsync(r => r.ChallengeDate == today && r.UserId == userId);

        if (existing != null)
        {
            _logger.LogWarning("User {UserId} already completed daily challenge for {Date}", userId, today);
            return;
        }

        var challengeResult = new DailyChallengeResult
        {
            ChallengeDate = today,
            UserId = userId,
            GameId = gameId,
            Score = score,
            Result = result,
            CompletedAt = DateTime.UtcNow
        };

        _dbContext.DailyChallengeResults.Add(challengeResult);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Recorded daily challenge result for user {UserId}: {Result}, score {Score}",
            userId, result, score);
    }

    public async Task<IReadOnlyList<DailyChallengeLeaderboardEntry>> GetDailyLeaderboardAsync(DateTime date, int top = 10)
    {
        var results = await _dbContext.DailyChallengeResults
            .Where(r => r.ChallengeDate == date.Date)
            .Join(_dbContext.Users,
                r => r.UserId,
                u => u.UniqueId,
                (r, u) => new { r.Score, r.Result, u.Name })
            .OrderByDescending(x => x.Score)
            .Take(top)
            .ToListAsync();

        return results.Select((x, i) => new DailyChallengeLeaderboardEntry
        {
            Rank = i + 1,
            PlayerName = x.Name,
            Score = x.Score,
            Result = x.Result.ToString()
        }).ToList();
    }
}
