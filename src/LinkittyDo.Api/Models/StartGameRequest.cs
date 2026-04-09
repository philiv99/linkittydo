using System.ComponentModel.DataAnnotations;

namespace LinkittyDo.Api.Models;

/// <summary>
/// Request to start a new game
/// </summary>
public class StartGameRequest
{
    /// <summary>
    /// Optional user ID (null for guest users)
    /// </summary>
    [StringLength(50, ErrorMessage = "UserId cannot exceed 50 characters")]
    [RegularExpression(@"^(USR-\d+-[A-Z0-9]+)?$", ErrorMessage = "Invalid user ID format")]
    public string? UserId { get; set; }
    
    /// <summary>
    /// Difficulty level (0-100, default 10)
    /// </summary>
    [Range(0, 100, ErrorMessage = "Difficulty must be between 0 and 100")]
    public int Difficulty { get; set; } = 10;
}
