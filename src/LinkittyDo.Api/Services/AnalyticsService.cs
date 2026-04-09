using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly LinkittyDoDbContext _context;

    public AnalyticsService(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task RecomputePlayerStatsAsync(string userId)
    {
        var games = await _context.GameRecords
            .Where(g => g.UserId == userId && g.Result != GameResult.InProgress)
            .ToListAsync();

        if (games.Count == 0) return;

        var gameIds = games.Select(g => g.GameId).ToList();
        var events = await _context.GameEvents
            .Where(e => gameIds.Contains(e.GameId))
            .ToListAsync();

        var solved = games.Count(g => g.Result == GameResult.Solved);
        var gaveUp = games.Count(g => g.Result == GameResult.GaveUp);
        var scores = games.Select(g => g.Score).ToList();

        var eventsByGame = events.GroupBy(e => e.GameId).ToDictionary(g => g.Key, g => g.ToList());
        var cluesPerGame = games
            .Select(g => eventsByGame.GetValueOrDefault(g.GameId, new())?.Count(e => e is ClueEvent) ?? 0)
            .ToList();
        var guessesPerGame = games
            .Select(g => eventsByGame.GetValueOrDefault(g.GameId, new())?.Count(e => e is GuessEvent) ?? 0)
            .ToList();

        // Compute streak from most recent games
        var orderedGames = games.OrderByDescending(g => g.CompletedAt ?? g.PlayedAt).ToList();
        int currentStreak = 0;
        foreach (var game in orderedGames)
        {
            if (game.Result == GameResult.Solved) currentStreak++;
            else break;
        }

        var existing = await _context.PlayerStats.FindAsync(userId);
        if (existing == null)
        {
            existing = new PlayerStats { UserId = userId };
            _context.PlayerStats.Add(existing);
        }

        existing.GamesPlayed = games.Count;
        existing.GamesSolved = solved;
        existing.GamesGaveUp = gaveUp;
        existing.AvgScore = scores.Count > 0 ? (decimal)scores.Average() : 0;
        existing.AvgCluesPerGame = cluesPerGame.Count > 0 ? (decimal)cluesPerGame.Average() : 0;
        existing.AvgGuessesPerGame = guessesPerGame.Count > 0 ? (decimal)guessesPerGame.Average() : 0;
        existing.BestScore = scores.Count > 0 ? scores.Max() : 0;
        existing.CurrentStreak = currentStreak;
        existing.BestStreak = Math.Max(existing.BestStreak, currentStreak);
        existing.LastPlayedAt = orderedGames.First().CompletedAt ?? orderedGames.First().PlayedAt;
        existing.ComputedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task RecomputePhrasePlayStatsAsync(string phraseUniqueId)
    {
        var games = await _context.GameRecords
            .Where(g => g.PhraseText == phraseUniqueId && g.Result != GameResult.InProgress)
            .ToListAsync();

        if (games.Count == 0) return;

        var gameIds = games.Select(g => g.GameId).ToList();
        var events = await _context.GameEvents
            .Where(e => gameIds.Contains(e.GameId))
            .ToListAsync();
        var eventsByGame = events.GroupBy(e => e.GameId).ToDictionary(g => g.Key, g => g.ToList());

        if (games.Count == 0) return;

        var solved = games.Count(g => g.Result == GameResult.Solved);
        var gaveUp = games.Count(g => g.Result == GameResult.GaveUp);
        var total = games.Count;

        var solvedGames = games.Where(g => g.Result == GameResult.Solved).ToList();
        var avgClues = solvedGames.Count > 0
            ? (decimal)solvedGames.Average(g => eventsByGame.GetValueOrDefault(g.GameId, new()).Count(e => e is ClueEvent))
            : (decimal?)null;
        var avgTime = solvedGames.Count > 0 && solvedGames.All(g => g.CompletedAt.HasValue)
            ? (decimal?)solvedGames.Average(g => (g.CompletedAt!.Value - g.PlayedAt).TotalSeconds)
            : null;

        // Calibrate difficulty based on solve rate: 0-100 scale
        int? calibrated = total >= 5 ? (int)Math.Round((1.0 - (double)solved / total) * 100) : null;

        var existing = await _context.PhrasePlayStats.FindAsync(phraseUniqueId);
        if (existing == null)
        {
            existing = new PhrasePlayStats { PhraseUniqueId = phraseUniqueId };
            _context.PhrasePlayStats.Add(existing);
        }

        existing.TimesPlayed = total;
        existing.TimesSolved = solved;
        existing.TimesGaveUp = gaveUp;
        existing.SolveRate = total > 0 ? (decimal)solved / total : 0;
        existing.AvgCluesToSolve = avgClues;
        existing.AvgTimeToSolveSeconds = avgTime;
        existing.GiveUpRate = total > 0 ? (decimal)gaveUp / total : 0;
        existing.CalibratedDifficulty = calibrated;
        existing.LastComputedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task RecomputeClueEffectivenessAsync()
    {
        var allEvents = await _context.GameEvents.ToListAsync();
        var clueEvents = allEvents.OfType<ClueEvent>().ToList();
        var guessEvents = allEvents.OfType<GuessEvent>().Where(g => g.IsCorrect).ToList();

        // Group clues by (SearchTerm, UrlDomain) — extract domain from URL
        var clueGroups = clueEvents
            .GroupBy(c => new { c.SearchTerm, Domain = ExtractDomain(c.Url) })
            .ToList();

        foreach (var group in clueGroups)
        {
            var targetWords = group.Select(c => c.WordIndex).Distinct().ToList();
            var firstClue = group.First();
            var timesShown = group.Count();

            // Count how many times a correct guess followed this clue in the same game
            var timesLedToCorrect = 0;
            foreach (var clue in group)
            {
                var correctAfter = guessEvents.Any(g =>
                    g.GameId == clue.GameId &&
                    g.WordIndex == clue.WordIndex &&
                    g.SequenceNumber > clue.SequenceNumber);
                if (correctAfter) timesLedToCorrect++;
            }

            var existing = await _context.ClueEffectiveness
                .FirstOrDefaultAsync(c =>
                    c.SearchTerm == group.Key.SearchTerm &&
                    c.UrlDomain == group.Key.Domain);

            if (existing == null)
            {
                existing = new ClueEffectiveness
                {
                    TargetWord = firstClue.SearchTerm,
                    SearchTerm = group.Key.SearchTerm,
                    UrlDomain = group.Key.Domain
                };
                _context.ClueEffectiveness.Add(existing);
            }

            existing.TimesShown = timesShown;
            existing.TimesLedToCorrectGuess = timesLedToCorrect;
            existing.LastComputedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<PlayerStats?> GetPlayerStatsAsync(string userId)
    {
        return await _context.PlayerStats.FindAsync(userId);
    }

    public async Task<PhrasePlayStats?> GetPhrasePlayStatsAsync(string phraseUniqueId)
    {
        return await _context.PhrasePlayStats.FindAsync(phraseUniqueId);
    }

    public async Task<IReadOnlyList<ClueEffectiveness>> GetTopCluesAsync(string targetWord, int top = 5)
    {
        return await _context.ClueEffectiveness
            .Where(c => c.TargetWord == targetWord)
            .OrderByDescending(c => c.TimesLedToCorrectGuess)
            .Take(top)
            .ToListAsync();
    }

    private static string ExtractDomain(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Host;
        return url;
    }
}
