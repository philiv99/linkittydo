using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Services;

public class SimulationService : ISimulationService
{
    private readonly LinkittyDoDbContext _context;
    private readonly Random _random = new();

    public SimulationService(LinkittyDoDbContext context)
    {
        _context = context;
    }

    public async Task<SimulationResult> SimulateGameAsync(int profileId, string? phraseUniqueId = null)
    {
        var profile = await _context.SimulationProfiles.FindAsync(profileId)
            ?? throw new ArgumentException($"Simulation profile {profileId} not found");

        // Get a phrase to play
        var phrase = phraseUniqueId != null
            ? await _context.GamePhrases.FirstOrDefaultAsync(p => p.UniqueId == phraseUniqueId)
            : await _context.GamePhrases
                .Where(p => p.Difficulty >= profile.PreferredDifficulty - 20 && p.Difficulty <= profile.PreferredDifficulty + 20)
                .OrderBy(_ => Guid.NewGuid())
                .FirstOrDefaultAsync()
            ?? await _context.GamePhrases.OrderBy(_ => Guid.NewGuid()).FirstOrDefaultAsync();

        if (phrase == null)
            throw new InvalidOperationException("No phrases available for simulation");

        // Create simulated user (or reuse)
        var simUserId = SimulationIdGenerator.GenerateSimUserId();
        var simUser = new User
        {
            UniqueId = simUserId,
            Name = $"SimBot_{profile.Name}_{_random.Next(1000, 9999)}",
            Email = $"{simUserId.ToLowerInvariant()}@simulated.local",
            IsSimulated = true,
            PreferredDifficulty = profile.PreferredDifficulty,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(simUser);

        // Create game
        var gameId = SimulationIdGenerator.GenerateSimGameId();
        var now = DateTime.UtcNow;
        var game = new GameRecord
        {
            GameId = gameId,
            UserId = simUserId,
            PhraseId = 0,
            PhraseText = phrase.UniqueId,
            Difficulty = phrase.Difficulty,
            IsSimulated = true,
            PlayedAt = now,
            Result = GameResult.InProgress
        };

        var events = new List<GameEvent>();
        int seq = 0;
        int score = 0;
        int clueCount = 0;
        int guessCount = 0;
        var words = phrase.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool gaveUp = false;

        for (int i = 0; i < words.Length && !gaveUp; i++)
        {
            // Decide if player requests a clue
            if ((decimal)_random.NextDouble() < profile.ClueProbability)
            {
                clueCount++;
                events.Add(new ClueEvent
                {
                    GameId = gameId,
                    SequenceNumber = seq++,
                    WordIndex = i,
                    SearchTerm = words[i],
                    Url = $"https://simulated.example.com/{words[i]}",
                    Timestamp = now.AddSeconds(seq * profile.AvgActionDelaySeconds)
                });
            }

            // Decide if player gives up
            if ((decimal)_random.NextDouble() < profile.GiveUpProbability)
            {
                gaveUp = true;
                events.Add(new GameEndEvent
                {
                    GameId = gameId,
                    SequenceNumber = seq++,
                    Reason = "gaveup",
                    Timestamp = now.AddSeconds(seq * profile.AvgActionDelaySeconds)
                });
                break;
            }

            // Player guesses
            bool correct = (decimal)_random.NextDouble() < profile.CorrectGuessProbability;
            guessCount++;
            int pts = correct ? 100 : 0;
            score += pts;
            events.Add(new GuessEvent
            {
                GameId = gameId,
                SequenceNumber = seq++,
                WordIndex = i,
                GuessText = correct ? words[i] : "wrong_guess",
                IsCorrect = correct,
                PointsAwarded = pts,
                Timestamp = now.AddSeconds(seq * profile.AvgActionDelaySeconds)
            });
        }

        if (!gaveUp)
        {
            events.Add(new GameEndEvent
            {
                GameId = gameId,
                SequenceNumber = seq++,
                Reason = "solved",
                Timestamp = now.AddSeconds(seq * profile.AvgActionDelaySeconds)
            });
            game.Result = GameResult.Solved;
        }
        else
        {
            game.Result = GameResult.GaveUp;
        }

        game.Score = score;
        game.CompletedAt = now.AddSeconds(seq * profile.AvgActionDelaySeconds);

        _context.GameRecords.Add(game);
        _context.GameEvents.AddRange(events);
        await _context.SaveChangesAsync();

        return new SimulationResult
        {
            GameId = gameId,
            UserId = simUserId,
            Result = game.Result,
            Score = score,
            ClueCount = clueCount,
            GuessCount = guessCount
        };
    }

    public async Task<BatchSimulationResult> RunBatchAsync(int profileId, int count, string? phraseUniqueId = null)
    {
        var results = new List<SimulationResult>();
        for (int i = 0; i < count; i++)
        {
            var result = await SimulateGameAsync(profileId, phraseUniqueId);
            results.Add(result);
        }

        return new BatchSimulationResult
        {
            TotalGames = results.Count,
            Solved = results.Count(r => r.Result == GameResult.Solved),
            GaveUp = results.Count(r => r.Result == GameResult.GaveUp),
            AvgScore = results.Count > 0 ? results.Average(r => r.Score) : 0,
            Games = results
        };
    }

    public async Task<PurgeResult> PurgeSimulationDataAsync()
    {
        // Delete events for simulated games
        var simGameIds = await _context.GameRecords
            .Where(g => g.IsSimulated)
            .Select(g => g.GameId)
            .ToListAsync();

        var eventsDeleted = 0;
        if (simGameIds.Count > 0)
        {
            var events = await _context.GameEvents
                .Where(e => simGameIds.Contains(e.GameId))
                .ToListAsync();
            eventsDeleted = events.Count;
            _context.GameEvents.RemoveRange(events);
        }

        // Delete simulated games
        var simGames = await _context.GameRecords.Where(g => g.IsSimulated).ToListAsync();
        var gamesDeleted = simGames.Count;
        _context.GameRecords.RemoveRange(simGames);

        // Delete simulated users
        var simUsers = await _context.Users.Where(u => u.IsSimulated).ToListAsync();
        var usersDeleted = simUsers.Count;
        _context.Users.RemoveRange(simUsers);

        // Delete related player stats
        var simUserIds = simUsers.Select(u => u.UniqueId).ToList();
        if (simUserIds.Count > 0)
        {
            var simStats = await _context.PlayerStats
                .Where(s => simUserIds.Contains(s.UserId))
                .ToListAsync();
            _context.PlayerStats.RemoveRange(simStats);
        }

        await _context.SaveChangesAsync();

        return new PurgeResult
        {
            UsersDeleted = usersDeleted,
            GamesDeleted = gamesDeleted,
            EventsDeleted = eventsDeleted
        };
    }

    public async Task<IReadOnlyList<SimulationProfile>> GetProfilesAsync()
    {
        return await _context.SimulationProfiles
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
