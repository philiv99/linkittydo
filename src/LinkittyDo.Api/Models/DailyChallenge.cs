namespace LinkittyDo.Api.Models;

public class DailyChallenge
{
    public DateTime Date { get; set; }
    public string PhraseUniqueId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DailyChallengeResult
{
    public int Id { get; set; }
    public DateTime ChallengeDate { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public int Score { get; set; }
    public GameResult Result { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class DailyChallengeResponse
{
    public DateTime Date { get; set; }
    public string PhraseUniqueId { get; set; } = string.Empty;
    public bool AlreadyPlayed { get; set; }
    public DailyChallengeResultResponse? PreviousResult { get; set; }
}

public class DailyChallengeResultResponse
{
    public int Score { get; set; }
    public string Result { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

public class DailyChallengeLeaderboardEntry
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Result { get; set; } = string.Empty;
}
