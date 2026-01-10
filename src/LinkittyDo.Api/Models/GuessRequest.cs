namespace LinkittyDo.Api.Models;

public class GuessRequest
{
    public int WordIndex { get; set; }
    public string Guess { get; set; } = string.Empty;
}

public class GuessResponse
{
    public bool IsCorrect { get; set; }
    public bool IsPhraseComplete { get; set; }
    public int CurrentScore { get; set; }
    public string? RevealedWord { get; set; }
}
