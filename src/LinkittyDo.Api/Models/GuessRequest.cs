using System.ComponentModel.DataAnnotations;

namespace LinkittyDo.Api.Models;

public class GuessRequest
{
    [Required]
    [Range(0, 100, ErrorMessage = "Word index must be between 0 and 100")]
    public int WordIndex { get; set; }

    [Required(ErrorMessage = "Guess is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Guess must be between 1 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s'-]+$", ErrorMessage = "Guess can only contain letters, numbers, spaces, apostrophes, and hyphens")]
    public string Guess { get; set; } = string.Empty;
}

public class GuessResponse
{
    public bool IsCorrect { get; set; }
    public bool IsPhraseComplete { get; set; }
    public int CurrentScore { get; set; }
    public string? RevealedWord { get; set; }
}
