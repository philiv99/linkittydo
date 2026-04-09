using LinkittyDo.Api.Services;

namespace LinkittyDo.Api.Tests;

public class ClueServiceDifficultyTests
{
    // --- SelectSynonymByDifficulty tests ---

    private static List<ClueService.ScoredWord> CreateScoredWords()
    {
        return new List<ClueService.ScoredWord>
        {
            new() { Word = "closest", Score = 1000 },
            new() { Word = "close", Score = 800 },
            new() { Word = "mid_high", Score = 600 },
            new() { Word = "mid", Score = 400 },
            new() { Word = "mid_low", Score = 300 },
            new() { Word = "distant", Score = 200 },
            new() { Word = "very_distant", Score = 100 },
            new() { Word = "most_distant", Score = 50 },
            new() { Word = "obscure", Score = 10 }
        };
    }

    [Fact]
    public void SelectSynonymByDifficulty_Easy_SelectsFromTopThird()
    {
        var words = CreateScoredWords();
        var results = new HashSet<string>();

        // Run multiple times to get distribution
        for (int i = 0; i < 100; i++)
        {
            results.Add(ClueService.SelectSynonymByDifficulty(words, 10));
        }

        // Top third = first 3 words (closest, close, mid_high)
        var topThird = new HashSet<string> { "closest", "close", "mid_high" };
        Assert.All(results, r => Assert.Contains(r, topThird));
    }

    [Fact]
    public void SelectSynonymByDifficulty_Expert_SelectsFromBottomThird()
    {
        var words = CreateScoredWords();
        var results = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            results.Add(ClueService.SelectSynonymByDifficulty(words, 90));
        }

        // Bottom third = last 3 words (most_distant, obscure, very_distant)
        var bottomThird = new HashSet<string> { "most_distant", "obscure", "very_distant" };
        Assert.All(results, r => Assert.Contains(r, bottomThird));
    }

    [Fact]
    public void SelectSynonymByDifficulty_SingleWord_ReturnsThatWord()
    {
        var words = new List<ClueService.ScoredWord>
        {
            new() { Word = "only", Score = 500 }
        };

        var result = ClueService.SelectSynonymByDifficulty(words, 50);

        Assert.Equal("only", result);
    }

    [Fact]
    public void SelectSynonymByDifficulty_EmptyList_ReturnsEmpty()
    {
        var result = ClueService.SelectSynonymByDifficulty(new List<ClueService.ScoredWord>(), 50);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SelectSynonymByDifficulty_Hard_SelectsFromBottomHalf()
    {
        var words = CreateScoredWords();
        var results = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            results.Add(ClueService.SelectSynonymByDifficulty(words, 60));
        }

        // Bottom half: mid_low, distant, very_distant, most_distant, obscure (indices 4-8)
        var bottomHalf = new HashSet<string> { "mid", "mid_low", "distant", "very_distant", "most_distant", "obscure" };
        Assert.All(results, r => Assert.Contains(r, bottomHalf));
    }

    // --- SelectUrlByDifficulty tests ---

    [Fact]
    public void SelectUrlByDifficulty_Easy_PrefersTransparentUrls()
    {
        var urls = new List<string>
        {
            "https://en.wikipedia.org/wiki/Fast",
            "https://www.merriam-webster.com/dictionary/fast",
            "https://randomsite.com/about-fast",
            "https://obscure-blog.net/post/123"
        };

        var results = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            results.Add(ClueService.SelectUrlByDifficulty(urls, 10));
        }

        // Easy should only pick transparent URLs (wikipedia, merriam-webster)
        Assert.All(results, r => Assert.True(
            r.Contains("wikipedia.org") || r.Contains("merriam-webster.com"),
            $"Easy mode selected non-transparent URL: {r}"));
    }

    [Fact]
    public void SelectUrlByDifficulty_Hard_PrefersOpaqueUrls()
    {
        var urls = new List<string>
        {
            "https://en.wikipedia.org/wiki/Fast",
            "https://www.merriam-webster.com/dictionary/fast",
            "https://randomsite.com/about-fast",
            "https://obscure-blog.net/post/123"
        };

        var results = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            results.Add(ClueService.SelectUrlByDifficulty(urls, 70));
        }

        // Hard should only pick opaque URLs
        Assert.All(results, r => Assert.True(
            !r.Contains("wikipedia.org") && !r.Contains("merriam-webster.com"),
            $"Hard mode selected transparent URL: {r}"));
    }

    [Fact]
    public void SelectUrlByDifficulty_Medium_PicksAnyUrl()
    {
        var urls = new List<string>
        {
            "https://en.wikipedia.org/wiki/Fast",
            "https://randomsite.com/about-fast"
        };

        // Medium (30) should pick from all URLs
        var result = ClueService.SelectUrlByDifficulty(urls, 30);
        Assert.Contains(result, urls);
    }

    [Fact]
    public void SelectUrlByDifficulty_Easy_FallsBackToAll_WhenNoTransparentUrls()
    {
        var urls = new List<string>
        {
            "https://randomsite.com/about-fast",
            "https://obscure-blog.net/post/123"
        };

        var result = ClueService.SelectUrlByDifficulty(urls, 10);

        // No transparent URLs, so it should pick from all
        Assert.Contains(result, urls);
    }

    [Fact]
    public void SelectUrlByDifficulty_SingleUrl_ReturnsThatUrl()
    {
        var urls = new List<string> { "https://example.com/page" };

        var result = ClueService.SelectUrlByDifficulty(urls, 50);

        Assert.Equal("https://example.com/page", result);
    }

    [Fact]
    public void SelectUrlByDifficulty_EmptyList_ReturnsEmpty()
    {
        var result = ClueService.SelectUrlByDifficulty(new List<string>(), 50);

        Assert.Equal(string.Empty, result);
    }
}
