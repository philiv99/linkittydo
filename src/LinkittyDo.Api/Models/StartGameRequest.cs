namespace LinkittyDo.Api.Models;

/// <summary>
/// Request to start a new game
/// </summary>
public class StartGameRequest
{
    /// <summary>
    /// Optional user ID (null for guest users)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Difficulty level (0-100, default 10)
    /// </summary>
    public int Difficulty { get; set; } = 10;
}
