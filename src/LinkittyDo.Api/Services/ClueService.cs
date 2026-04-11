using LinkittyDo.Api.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace LinkittyDo.Api.Services;

public interface IClueService
{
    Task<ClueResponse> GetClueAsync(GameSession session, int wordIndex);
}

public class ClueService : IClueService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClueService> _logger;
    private readonly Random _random = new();
    private static readonly ConcurrentDictionary<string, CachedSynonyms> _synonymCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    // URL domains considered "transparent" (easy to deduce meaning from)
    private static readonly HashSet<string> TransparentDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "wikipedia.org", "wiktionary.org", "merriam-webster.com", "dictionary.com",
        "thesaurus.com", "britannica.com", "oxford", "cambridge.org"
    };

    public ClueService(HttpClient httpClient, ILogger<ClueService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// Generates a clue URL by searching for a synonym and returning an actual webpage URL.
    /// </summary>
    public async Task<ClueResponse> GetClueAsync(GameSession session, int wordIndex)
    {
        var word = session.Phrase.Words.FirstOrDefault(w => w.Index == wordIndex);
        if (word == null || !word.IsHidden)
        {
            return new ClueResponse { Url = string.Empty, SearchTerm = string.Empty };
        }

        // Initialize used terms set for this word if not exists
        if (!session.UsedClueTerms.ContainsKey(wordIndex))
        {
            session.UsedClueTerms[wordIndex] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Get a synonym that hasn't been used yet, considering difficulty
        _logger.LogInformation("Getting clue for word '{Word}' at index {WordIndex}, difficulty {Difficulty}", 
            word.Text, wordIndex, session.Difficulty);
        
        // Build context from neighboring words for disambiguation
        var leftContext = GetLeftContext(session.Phrase.Words, wordIndex);
        var rightContext = GetRightContext(session.Phrase.Words, wordIndex);
        
        var searchResult = await GetUnusedSynonymAsync(word.Text, session.UsedClueTerms[wordIndex], session.Difficulty, leftContext, rightContext);
        var searchTerm = searchResult.Term;
        var relationshipType = searchResult.RelationshipType;
        
        // Track that we've used this term
        session.UsedClueTerms[wordIndex].Add(searchTerm);
        _logger.LogInformation("Selected search term '{SearchTerm}' ({RelationshipType}) for word '{Word}'", searchTerm, relationshipType, word.Text);

        // Search for actual URLs using the synonym, considering difficulty
        var clueUrl = await GetSearchResultUrlAsync(searchTerm, session.UsedClueUrls, session.Difficulty);
        
        // Track that we've used this URL
        if (!string.IsNullOrEmpty(clueUrl))
        {
            session.UsedClueUrls.Add(clueUrl);
            _logger.LogInformation("Selected clue URL '{ClueUrl}' for search term '{SearchTerm}'", clueUrl, searchTerm);
        }
        else
        {
            _logger.LogWarning("No valid URL found for search term '{SearchTerm}'", searchTerm);
        }

        return new ClueResponse
        {
            Url = clueUrl,
            SearchTerm = searchTerm,
            RelationshipType = relationshipType
        };
    }

    private async Task<string> GetSearchResultUrlAsync(string searchTerm, HashSet<string> usedUrls, int difficulty = 10)
    {
        try
        {
            // Use DuckDuckGo HTML lite version to get search results
            var searchUrl = $"https://html.duckduckgo.com/html/?q={HttpUtility.UrlEncode(searchTerm)}";
            _logger.LogDebug("Searching DuckDuckGo with URL: {SearchUrl}", searchUrl);
            var html = await _httpClient.GetStringAsync(searchUrl);

            // Parse result URLs from DuckDuckGo HTML
            var urls = ParseDuckDuckGoResults(html);
            _logger.LogDebug("DuckDuckGo returned {UrlCount} URLs for '{SearchTerm}'", urls.Count, searchTerm);
            
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                foreach (var url in urls.Take(10))
                {
                    _logger.LogTrace("  Search result: {Url}", url);
                }
            }

            // Filter out already used URLs and pick based on difficulty
            var availableUrls = urls
                .Where(u => !usedUrls.Contains(u))
                .Where(u => !u.Contains("duckduckgo.com"))
                .Where(u => IsValidClueUrl(u))
                .ToList();
            
            _logger.LogDebug("{AvailableCount} URLs available after filtering (excluded {UsedCount} already used)", 
                availableUrls.Count, usedUrls.Count);

            if (availableUrls.Count > 0)
            {
                var selectedUrl = SelectUrlByDifficulty(availableUrls, difficulty);
                _logger.LogDebug("Selected URL for difficulty {Difficulty}: {SelectedUrl}", difficulty, selectedUrl);
                return selectedUrl;
            }

            // Fallback: return first available URL even if used
            var fallbackUrl = urls.FirstOrDefault(u => IsValidClueUrl(u));
            if (fallbackUrl != null)
            {
                _logger.LogInformation("Using fallback URL (may be reused): {FallbackUrl}", fallbackUrl);
                return fallbackUrl;
            }
            
            var wikipediaFallback = $"https://en.wikipedia.org/wiki/{HttpUtility.UrlEncode(searchTerm)}";
            _logger.LogInformation("No search results found, using Wikipedia fallback: {WikiUrl}", wikipediaFallback);
            return wikipediaFallback;
        }
        catch (Exception ex)
        {
            // Fallback to Wikipedia if search fails
            _logger.LogWarning(ex, "Search failed for '{SearchTerm}', falling back to Wikipedia", searchTerm);
            return $"https://en.wikipedia.org/wiki/{HttpUtility.UrlEncode(searchTerm)}";
        }
    }

    private List<string> ParseDuckDuckGoResults(string html)
    {
        var urls = new List<string>();
        
        // DuckDuckGo HTML results have links in format: <a rel="nofollow" class="result__a" href="...">
        // The href contains a redirect URL with the actual URL encoded
        var linkPattern = new Regex(@"class=""result__a""\s+href=""([^""]+)""", RegexOptions.IgnoreCase);
        var matches = linkPattern.Matches(html);

        foreach (Match match in matches)
        {
            var href = match.Groups[1].Value;
            
            // DuckDuckGo uses redirect URLs, extract the actual URL
            var actualUrl = ExtractActualUrl(href);
            if (!string.IsNullOrEmpty(actualUrl))
            {
                urls.Add(actualUrl);
            }
        }

        // Also try to find direct URLs in result snippets
        var directLinkPattern = new Regex(@"href=""(https?://[^""]+)""", RegexOptions.IgnoreCase);
        var directMatches = directLinkPattern.Matches(html);
        
        foreach (Match match in directMatches)
        {
            var url = HttpUtility.HtmlDecode(match.Groups[1].Value);
            if (IsValidClueUrl(url) && !url.Contains("duckduckgo.com"))
            {
                urls.Add(url);
            }
        }

        return urls.Distinct().ToList();
    }

    private string ExtractActualUrl(string duckDuckGoUrl)
    {
        try
        {
            // DDG format: //duckduckgo.com/l/?uddg=ENCODED_URL&rut=...
            if (duckDuckGoUrl.Contains("uddg="))
            {
                var uddgMatch = Regex.Match(duckDuckGoUrl, @"uddg=([^&]+)");
                if (uddgMatch.Success)
                {
                    return HttpUtility.UrlDecode(uddgMatch.Groups[1].Value);
                }
            }
            
            // If it's already a direct URL
            if (duckDuckGoUrl.StartsWith("http"))
            {
                return duckDuckGoUrl;
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private bool IsValidClueUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (!url.StartsWith("http")) return false;
        if (url.Contains("duckduckgo.com")) return false;
        if (url.Contains("google.com/search")) return false;
        if (url.Contains("bing.com/search")) return false;
        
        // Prefer educational/reference sites
        return true;
    }

    private async Task<(string Term, string RelationshipType)> GetUnusedSynonymAsync(string word, HashSet<string> usedTerms, int difficulty = 10, string? leftContext = null, string? rightContext = null)
    {
        try
        {
            var synonyms = await GetSynonymsFromDatamuseAsync(word, difficulty, leftContext, rightContext);
            _logger.LogDebug("Datamuse returned {SynonymCount} synonyms for '{Word}' at difficulty {Difficulty}", 
                synonyms.Count, word, difficulty);
            
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("All synonyms for '{Word}': {Synonyms}", word, string.Join(", ", synonyms.Select(s => s.Word)));
            }
            
            var availableSynonyms = synonyms
                .Where(s => !s.Word.Equals(word, StringComparison.OrdinalIgnoreCase))
                .Where(s => !usedTerms.Contains(s.Word))
                .ToList();
            
            _logger.LogDebug("{AvailableCount} synonyms available after filtering (already used: {UsedTerms})", 
                availableSynonyms.Count, string.Join(", ", usedTerms));

            if (availableSynonyms.Count > 0)
            {
                // Select synonym based on difficulty:
                // Easy: pick from top-scored (most similar) synonyms
                // Hard: pick from bottom-scored (most distant) synonyms
                var selected = SelectScoredWordByDifficulty(availableSynonyms, difficulty);
                _logger.LogInformation("Selected synonym '{Synonym}' for word '{Word}' (difficulty {Difficulty}, from {Count} available)", 
                    selected.Word, word, difficulty, availableSynonyms.Count);
                return (selected.Word, selected.RelationshipType);
            }
            // Fallback: use the word itself if no synonyms available
            _logger.LogInformation("No unused synonyms available for '{Word}', using original word", word);
            return (word, "direct");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get synonyms for '{Word}', using fallback", word);
            return (word, "direct");
        }
    }

    /// <summary>
    /// Selects a synonym based on difficulty. Easy prefers close matches (high score),
    /// hard prefers distant matches (low score).
    /// </summary>
    internal static string SelectSynonymByDifficulty(List<ScoredWord> synonyms, int difficulty)
    {
        return SelectScoredWordByDifficulty(synonyms, difficulty).Word;
    }

    internal static ScoredWord SelectScoredWordByDifficulty(List<ScoredWord> synonyms, int difficulty)
    {
        if (synonyms.Count == 0) return new ScoredWord();
        if (synonyms.Count == 1) return synonyms[0];

        // Sort by score descending (highest = most similar)
        var sorted = synonyms.OrderByDescending(s => s.Score).ToList();
        
        if (difficulty <= 20)
        {
            // Easy: pick from top third (closest synonyms)
            var topCount = Math.Max(1, sorted.Count / 3);
            return sorted[new Random().Next(topCount)];
        }
        else if (difficulty <= 50)
        {
            // Medium: pick from middle range
            var start = sorted.Count / 4;
            var end = sorted.Count * 3 / 4;
            var range = Math.Max(1, end - start);
            return sorted[start + new Random().Next(range)];
        }
        else if (difficulty <= 80)
        {
            // Hard: pick from bottom half (more distant)
            var bottomStart = sorted.Count / 2;
            var range = Math.Max(1, sorted.Count - bottomStart);
            return sorted[bottomStart + new Random().Next(range)];
        }
        else
        {
            // Expert: pick from bottom third (most distant)
            var bottomStart = sorted.Count * 2 / 3;
            var range = Math.Max(1, sorted.Count - bottomStart);
            return sorted[bottomStart + new Random().Next(range)];
        }
    }

    /// <summary>
    /// Selects a URL based on difficulty. Easy prefers transparent/educational sites,
    /// hard avoids them.
    /// </summary>
    internal static string SelectUrlByDifficulty(List<string> urls, int difficulty)
    {
        if (urls.Count == 0) return string.Empty;
        if (urls.Count == 1) return urls[0];

        var transparent = urls.Where(u => IsTransparentUrl(u)).ToList();
        var opaque = urls.Where(u => !IsTransparentUrl(u)).ToList();

        if (difficulty <= 20)
        {
            // Easy: prefer transparent URLs (Wikipedia, dictionaries)
            if (transparent.Count > 0)
                return transparent[new Random().Next(transparent.Count)];
        }
        else if (difficulty >= 51)
        {
            // Hard/Expert: prefer opaque URLs (avoid Wikipedia, dictionaries)
            if (opaque.Count > 0)
                return opaque[new Random().Next(opaque.Count)];
        }

        // Medium or fallback: pick randomly from all
        return urls[new Random().Next(urls.Count)];
    }

    private static bool IsTransparentUrl(string url)
    {
        return TransparentDomains.Any(domain => url.Contains(domain, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<ScoredWord>> GetSynonymsFromDatamuseAsync(string word, int difficulty = 10, string? leftContext = null, string? rightContext = null)
    {
        // Check cache first
        var cacheKey = $"{word}|{difficulty}|{leftContext}|{rightContext}";
        if (_synonymCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogDebug("Cache hit for synonym lookup: {CacheKey}", cacheKey);
            return cached.Synonyms;
        }

        var results = new List<ScoredWord>();
        var encodedWord = HttpUtility.UrlEncode(word);
        
        // Always fetch close synonyms
        var synonymUrl = $"https://api.datamuse.com/words?rel_syn={encodedWord}&max=15";
        
        // Add context parameters for disambiguation if available
        var contextualMlUrl = $"https://api.datamuse.com/words?ml={encodedWord}&max=15";
        if (!string.IsNullOrEmpty(leftContext))
            contextualMlUrl += $"&lc={HttpUtility.UrlEncode(leftContext)}";
        if (!string.IsNullOrEmpty(rightContext))
            contextualMlUrl += $"&rc={HttpUtility.UrlEncode(rightContext)}";
        
        var synonymTask = FetchDatamuseScoredWordsAsync(synonymUrl, "synonym");
        var similarTask = FetchDatamuseScoredWordsAsync(contextualMlUrl, "similar");
        
        var tasks = new List<Task<List<ScoredWord>>> { synonymTask, similarTask };
        
        // For medium+ difficulty: add triggers (associated words)
        if (difficulty > 50)
        {
            tasks.Add(FetchDatamuseScoredWordsAsync(
                $"https://api.datamuse.com/words?rel_trg={encodedWord}&max=10", "trigger"));
        }
        
        // For hard difficulty: add antonyms (opposite meaning = harder clue)
        if (difficulty > 60)
        {
            tasks.Add(FetchDatamuseScoredWordsAsync(
                $"https://api.datamuse.com/words?rel_ant={encodedWord}&max=8", "antonym"));
        }
        
        // For expert difficulty: add homophones (sound-alike = tricky clue)
        if (difficulty > 80)
        {
            tasks.Add(FetchDatamuseScoredWordsAsync(
                $"https://api.datamuse.com/words?rel_hom={encodedWord}&max=5", "homophone"));
        }

        await Task.WhenAll(tasks);

        foreach (var task in tasks)
        {
            results.AddRange(await task);
        }

        // Deduplicate by word, keeping highest score and first relationship type
        var deduped = results
            .GroupBy(w => w.Word, StringComparer.OrdinalIgnoreCase)
            .Select(g => { var best = g.OrderByDescending(x => x.Score).First(); return new ScoredWord { Word = g.Key, Score = best.Score, RelationshipType = best.RelationshipType }; })
            .ToList();
        
        // Cache the result
        _synonymCache[cacheKey] = new CachedSynonyms { Synonyms = deduped, ExpiresAt = DateTime.UtcNow.Add(CacheTtl) };
        _logger.LogDebug("Cached {Count} synonyms for '{Word}' (key: {CacheKey})", deduped.Count, word, cacheKey);
        
        return deduped;
    }

    private async Task<List<ScoredWord>> FetchDatamuseScoredWordsAsync(string url, string relationshipType = "synonym")
    {
        try
        {
            _logger.LogDebug("Fetching from Datamuse: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            var words = JsonSerializer.Deserialize<List<DatamuseWord>>(response);
            var result = words?
                .Where(w => !string.IsNullOrEmpty(w.word))
                .Select(w => new ScoredWord { Word = w.word, Score = w.score, RelationshipType = relationshipType })
                .ToList() ?? new List<ScoredWord>();
            _logger.LogDebug("Datamuse returned {Count} words", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from Datamuse: {Url}", url);
            return new List<ScoredWord>();
        }
    }

    private class DatamuseWord
    {
        public string word { get; set; } = string.Empty;
        public int score { get; set; }
    }

    internal class ScoredWord
    {
        public string Word { get; set; } = string.Empty;
        public int Score { get; set; }
        public string RelationshipType { get; set; } = "synonym";
    }

    private class CachedSynonyms
    {
        public List<ScoredWord> Synonyms { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    private static string? GetLeftContext(List<PhraseWord> words, int wordIndex)
    {
        var sorted = words.OrderBy(w => w.Index).ToList();
        var idx = sorted.FindIndex(w => w.Index == wordIndex);
        if (idx > 0)
            return sorted[idx - 1].Text.ToLowerInvariant();
        return null;
    }

    private static string? GetRightContext(List<PhraseWord> words, int wordIndex)
    {
        var sorted = words.OrderBy(w => w.Index).ToList();
        var idx = sorted.FindIndex(w => w.Index == wordIndex);
        if (idx >= 0 && idx < sorted.Count - 1)
            return sorted[idx + 1].Text.ToLowerInvariant();
        return null;
    }

    /// <summary>
    /// Clears the synonym cache. Used for testing.
    /// </summary>
    internal static void ClearCache()
    {
        _synonymCache.Clear();
    }

    /// <summary>
    /// Gets the current cache count. Used for testing.
    /// </summary>
    internal static int CacheCount => _synonymCache.Count;
}
