namespace LinkittyDo.Api.Models;

/// <summary>
/// Persistent database representation of a game session.
/// Maps to the GameSessions table for DB-backed session storage.
/// </summary>
public class GameSessionRecord
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string PhraseUniqueId { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Difficulty { get; set; }
    public string StateJson { get; set; } = "{}";
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}
