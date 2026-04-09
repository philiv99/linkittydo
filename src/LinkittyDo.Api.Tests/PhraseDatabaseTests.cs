using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace LinkittyDo.Api.Tests;

public class PhraseDatabaseTests
{
    private readonly JsonGamePhraseRepository _repository;

    public PhraseDatabaseTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DataDirectory", Path.Combine(AppContext.BaseDirectory, "Data") }
            })
            .Build();
        var logger = Mock.Of<ILogger<JsonGamePhraseRepository>>();
        _repository = new JsonGamePhraseRepository(config, logger);
    }

    [Fact]
    public async Task PhraseDatabase_HasAtLeast100Phrases()
    {
        var count = await _repository.GetCountAsync();
        Assert.True(count >= 100, $"Expected at least 100 phrases, found {count}");
    }

    [Fact]
    public async Task PhraseDatabase_HasNoDuplicateTexts()
    {
        var phrases = (await _repository.GetAllAsync()).ToList();
        var normalizedTexts = phrases
            .Select(p => p.Text.Trim().ToLowerInvariant().Replace("  ", " "))
            .ToList();
        var uniqueTexts = normalizedTexts.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        Assert.Equal(phrases.Count, uniqueTexts);
    }

    [Fact]
    public async Task PhraseDatabase_AllPhrasesHaveValidFormat()
    {
        var phrases = (await _repository.GetAllAsync()).ToList();
        
        foreach (var phrase in phrases)
        {
            Assert.False(string.IsNullOrWhiteSpace(phrase.UniqueId), 
                $"Phrase has empty UniqueId");
            Assert.StartsWith("PHR-", phrase.UniqueId);
            Assert.False(string.IsNullOrWhiteSpace(phrase.Text), 
                $"Phrase {phrase.UniqueId} has empty text");
            Assert.True(phrase.WordCount >= 2, 
                $"Phrase {phrase.UniqueId} has WordCount {phrase.WordCount}, expected >= 2");
        }
    }

    [Fact]
    public async Task ExistsByTextAsync_ExistingPhrase_ReturnsTrue()
    {
        var phrases = (await _repository.GetAllAsync()).ToList();
        if (phrases.Count == 0) return;

        var exists = await _repository.ExistsByTextAsync(phrases[0].Text);
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByTextAsync_NonexistentPhrase_ReturnsFalse()
    {
        var exists = await _repository.ExistsByTextAsync("This phrase absolutely does not exist xyz123");
        Assert.False(exists);
    }

    [Fact]
    public async Task GetByTextAsync_CaseInsensitive()
    {
        var phrases = (await _repository.GetAllAsync()).ToList();
        if (phrases.Count == 0) return;

        var original = phrases[0];
        var found = await _repository.GetByTextAsync(original.Text.ToUpperInvariant());
        Assert.NotNull(found);
        Assert.Equal(original.UniqueId, found.UniqueId);
    }
}
