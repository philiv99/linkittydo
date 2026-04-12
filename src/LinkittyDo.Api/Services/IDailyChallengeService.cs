using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IDailyChallengeService
{
    Task<DailyChallenge> GetOrCreateTodaysChallengeAsync();
    Task<DailyChallengeResponse> GetChallengeStatusAsync(string? userId);
    Task RecordChallengeResultAsync(string userId, string gameId, int score, GameResult result);
    Task<IReadOnlyList<DailyChallengeLeaderboardEntry>> GetDailyLeaderboardAsync(DateTime date, int top = 10);
}
