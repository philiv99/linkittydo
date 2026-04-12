using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LinkittyDo.Api.Tests;

public class PhraseScalingTests
{
    private static LinkittyDoDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LinkittyDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new LinkittyDoDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void ComputeDifficultyFromText_ReturnsNonZero_ForTypicalPhrase()
    {
        var difficulty = GamePhraseService.ComputeDifficultyFromText("the early bird catches the worm");
        Assert.True(difficulty > 0, $"Expected > 0, got {difficulty}");
        Assert.True(difficulty <= 100, $"Expected <= 100, got {difficulty}");
    }

    [Fact]
    public void ComputeDifficultyFromText_ShortPhrase_LowerThanLongPhrase()
    {
        var shortDifficulty = GamePhraseService.ComputeDifficultyFromText("time will tell");
        var longDifficulty = GamePhraseService.ComputeDifficultyFromText(
            "the journey of a thousand miles begins with a single step");

        Assert.True(longDifficulty > shortDifficulty,
            $"Long phrase ({longDifficulty}) should be harder than short ({shortDifficulty})");
    }

    [Fact]
    public void ComputeDifficultyFromText_DifficultVocabulary_HigherDifficulty()
    {
        var simpleDifficulty = GamePhraseService.ComputeDifficultyFromText("love is blind");
        var complexDifficulty = GamePhraseService.ComputeDifficultyFromText(
            "perseverance conquers all obstacles");

        Assert.True(complexDifficulty > simpleDifficulty,
            $"Complex phrase ({complexDifficulty}) should be harder than simple ({simpleDifficulty})");
    }

    [Fact]
    public async Task PhraseAdminService_CreateDuplicate_ThrowsPhraseExists()
    {
        using var context = CreateInMemoryContext();
        var service = new PhraseAdminService(context);

        await service.CreatePhraseAsync("break the ice");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreatePhraseAsync("break the ice"));
        Assert.Equal("PHRASE_EXISTS", ex.Message);
    }

    [Fact]
    public async Task PhraseAdminService_CreateDuplicate_CaseInsensitive()
    {
        using var context = CreateInMemoryContext();
        var service = new PhraseAdminService(context);

        await service.CreatePhraseAsync("Break The Ice");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreatePhraseAsync("break the ice"));
        Assert.Equal("PHRASE_EXISTS", ex.Message);
    }

    [Fact]
    public async Task PhraseAdminService_CreateUnique_Succeeds()
    {
        using var context = CreateInMemoryContext();
        var service = new PhraseAdminService(context);

        var phrase = await service.CreatePhraseAsync("a brand new phrase");

        Assert.NotNull(phrase);
        Assert.Equal("a brand new phrase", phrase.Text);
        Assert.StartsWith("PHR-", phrase.UniqueId);
    }

    [Fact]
    public void ComputeDifficultyFromText_AllStopWords_ReturnsLowValue()
    {
        // "it is what it is" — all tokens are stop words, no hidden words
        // Phrase length still contributes to difficulty score
        var difficulty = GamePhraseService.ComputeDifficultyFromText("it is what it is");
        Assert.True(difficulty <= 20, $"Expected <= 20 for all stop words, got {difficulty}");
    }

    [Fact]
    public void ComputeDifficultyFromText_SpansFullRange()
    {
        // Verify the curated phrases span a reasonable difficulty range
        var phrases = new[]
        {
            "less is more",
            "break the ice",
            "curiosity killed the cat",
            "the journey of a thousand miles begins with a single step",
            "perseverance conquers all obstacles"
        };

        var difficulties = phrases
            .Select(p => GamePhraseService.ComputeDifficultyFromText(p))
            .OrderBy(d => d)
            .ToList();

        var minDifficulty = difficulties.First();
        var maxDifficulty = difficulties.Last();
        var range = maxDifficulty - minDifficulty;

        Assert.True(range >= 15, $"Expected difficulty range >= 15, got {range} (min={minDifficulty}, max={maxDifficulty})");
    }
}
