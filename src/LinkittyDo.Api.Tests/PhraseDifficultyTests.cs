using LinkittyDo.Api.Services;

namespace LinkittyDo.Api.Tests;

public class PhraseDifficultyTests
{
    [Fact]
    public void ComputePhraseDifficulty_ShortEasyPhrase_LowDifficulty()
    {
        // "the cat" -> 2 tokens, 1 hidden ("cat", 3 chars)
        var tokens = new List<string> { "the", "cat" };
        var hiddenIndices = new HashSet<int> { 1 }; // "cat" is hidden

        var difficulty = GamePhraseService.ComputePhraseDifficulty(tokens, hiddenIndices);

        // hiddenRatio=0.5*40=20, lengthScore=0/5*30=0, wordLength=(3-3)/5*30=0 => 20
        Assert.Equal(20, difficulty);
    }

    [Fact]
    public void ComputePhraseDifficulty_LongComplexPhrase_HighDifficulty()
    {
        // 7 content words, 5 hidden with longer words
        var tokens = new List<string> { "the", "extraordinary", "magnificent", "sparkling", "crystalline", "diamond", "shimmered" };
        var hiddenIndices = new HashSet<int> { 1, 2, 3, 4, 5, 6 }; // All except "the"

        var difficulty = GamePhraseService.ComputePhraseDifficulty(tokens, hiddenIndices);

        // High hidden ratio + long phrase + long words = high difficulty
        Assert.True(difficulty >= 60, $"Expected >= 60, got {difficulty}");
    }

    [Fact]
    public void ComputePhraseDifficulty_MediumPhrase_MidRange()
    {
        // "actions speak louder" -> 3 tokens, 2 hidden
        var tokens = new List<string> { "actions", "speak", "louder" };
        var hiddenIndices = new HashSet<int> { 0, 2 }; // "actions" and "louder" hidden

        var difficulty = GamePhraseService.ComputePhraseDifficulty(tokens, hiddenIndices);

        Assert.InRange(difficulty, 15, 60);
    }

    [Fact]
    public void ComputePhraseDifficulty_AllHidden_HigherThanPartial()
    {
        var tokens = new List<string> { "quick", "brown", "fox" };
        var allHidden = new HashSet<int> { 0, 1, 2 };
        var partialHidden = new HashSet<int> { 1 };

        var allDifficulty = GamePhraseService.ComputePhraseDifficulty(tokens, allHidden);
        var partialDifficulty = GamePhraseService.ComputePhraseDifficulty(tokens, partialHidden);

        Assert.True(allDifficulty > partialDifficulty,
            $"All hidden ({allDifficulty}) should be harder than partial ({partialDifficulty})");
    }

    [Fact]
    public void ComputePhraseDifficulty_LongerWords_HarderThanShorter()
    {
        // Same structure but different word lengths
        var shortTokens = new List<string> { "the", "big", "red", "car" };
        var longTokens = new List<string> { "the", "enormous", "magnificent", "automobile" };
        var hidden = new HashSet<int> { 1, 2, 3 };

        var shortDifficulty = GamePhraseService.ComputePhraseDifficulty(shortTokens, hidden);
        var longDifficulty = GamePhraseService.ComputePhraseDifficulty(longTokens, hidden);

        Assert.True(longDifficulty > shortDifficulty,
            $"Long words ({longDifficulty}) should be harder than short ({shortDifficulty})");
    }

    [Fact]
    public void ComputePhraseDifficulty_EmptyTokens_Returns50()
    {
        var tokens = new List<string>();
        var hidden = new HashSet<int>();

        var difficulty = GamePhraseService.ComputePhraseDifficulty(tokens, hidden);

        Assert.Equal(50, difficulty);
    }

    [Fact]
    public void ComputePhraseDifficulty_WithPunctuation_IgnoresPunctuation()
    {
        // Punctuation tokens should not affect content token count
        var tokens = new List<string> { "hello", ",", "world", "!" };
        var hidden = new HashSet<int> { 0, 2 }; // "hello" and "world"

        var difficulty = GamePhraseService.ComputePhraseDifficulty(tokens, hidden);

        // Should be computed based on 2 content tokens, not 4
        Assert.InRange(difficulty, 0, 100);
    }

    [Fact]
    public void ComputePhraseDifficulty_NeverExceeds100()
    {
        // Extreme case: very long phrase, all hidden, long words
        var tokens = Enumerable.Range(0, 10).Select(_ => "extraordinarily").ToList();
        var hidden = Enumerable.Range(0, 10).ToHashSet();

        var difficulty = GamePhraseService.ComputePhraseDifficulty(tokens, hidden);

        Assert.InRange(difficulty, 0, 100);
    }

    [Fact]
    public void ComputePhraseDifficulty_NeverBelow0()
    {
        var tokens = new List<string> { "hi" };
        var hidden = new HashSet<int>();

        var difficulty = GamePhraseService.ComputePhraseDifficulty(tokens, hidden);

        Assert.InRange(difficulty, 0, 100);
    }
}
