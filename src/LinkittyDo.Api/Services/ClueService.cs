using LinkittyDo.Api.Models;
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

        // Get a synonym that hasn't been used yet
        _logger.LogInformation("Getting clue for word '{Word}' at index {WordIndex}", word.Text, wordIndex);
        var searchTerm = await GetUnusedSynonymAsync(word.Text, session.UsedClueTerms[wordIndex]);
        
        // Track that we've used this term
        session.UsedClueTerms[wordIndex].Add(searchTerm);
        _logger.LogInformation("Selected search term '{SearchTerm}' for word '{Word}'", searchTerm, word.Text);

        // Search for actual URLs using the synonym
        var clueUrl = await GetSearchResultUrlAsync(searchTerm, session.UsedClueUrls);
        
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
            SearchTerm = searchTerm
        };
    }

    private async Task<string> GetSearchResultUrlAsync(string searchTerm, HashSet<string> usedUrls)
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

            // Filter out already used URLs and pick a random one
            var availableUrls = urls
                .Where(u => !usedUrls.Contains(u))
                .Where(u => !u.Contains("duckduckgo.com"))
                .Where(u => IsValidClueUrl(u))
                .ToList();
            
            _logger.LogDebug("{AvailableCount} URLs available after filtering (excluded {UsedCount} already used)", 
                availableUrls.Count, usedUrls.Count);

            if (availableUrls.Count > 0)
            {
                var selectedUrl = availableUrls[_random.Next(availableUrls.Count)];
                _logger.LogDebug("Randomly selected URL from {Count} available: {SelectedUrl}", availableUrls.Count, selectedUrl);
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

    private async Task<string> GetUnusedSynonymAsync(string word, HashSet<string> usedTerms)
    {
        try
        {
            var synonyms = await GetSynonymsFromDatamuseAsync(word);
            _logger.LogDebug("Datamuse returned {SynonymCount} synonyms for '{Word}'", synonyms.Count, word);
            
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("All synonyms for '{Word}': {Synonyms}", word, string.Join(", ", synonyms));
            }
            
            var availableSynonyms = synonyms
                .Where(s => !s.Equals(word, StringComparison.OrdinalIgnoreCase))
                .Where(s => !usedTerms.Contains(s))
                .ToList();
            
            _logger.LogDebug("{AvailableCount} synonyms available after filtering (already used: {UsedTerms})", 
                availableSynonyms.Count, string.Join(", ", usedTerms));

            if (availableSynonyms.Count > 0)
            {
                var synonymWord = availableSynonyms[_random.Next(availableSynonyms.Count)];
                _logger.LogInformation("Selected synonym '{Synonym}' for word '{Word}' (from {Count} available)", 
                    synonymWord, word, availableSynonyms.Count);
                return synonymWord;
            }
            // Fallback: use the word itself with "meaning" if no synonyms available
            _logger.LogInformation("No unused synonyms available for '{Word}', using original word", word);
            return word;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get synonyms for '{Word}', using fallback", word);
            return word;
        }
    }

    private async Task<List<string>> GetSynonymsFromDatamuseAsync(string word)
    {
        var results = new List<string>();
        
        var synonymUrl = $"https://api.datamuse.com/words?rel_syn={HttpUtility.UrlEncode(word)}&max=15";
        var similarUrl = $"https://api.datamuse.com/words?ml={HttpUtility.UrlEncode(word)}&max=15";

        var synonymTask = FetchDatamuseWordsAsync(synonymUrl);
        var similarTask = FetchDatamuseWordsAsync(similarUrl);

        await Task.WhenAll(synonymTask, similarTask);

        results.AddRange(await synonymTask);
        results.AddRange(await similarTask);

        return results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<List<string>> FetchDatamuseWordsAsync(string url)
    {
        try
        {
            _logger.LogDebug("Fetching from Datamuse: {Url}", url);
            var response = await _httpClient.GetStringAsync(url);
            var words = JsonSerializer.Deserialize<List<DatamuseWord>>(response);
            var result = words?.Select(w => w.word).Where(w => !string.IsNullOrEmpty(w)).ToList() ?? new List<string>();
            _logger.LogDebug("Datamuse returned {Count} words", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from Datamuse: {Url}", url);
            return new List<string>();
        }
    }

    private class DatamuseWord
    {
        public string word { get; set; } = string.Empty;
        public int score { get; set; }
    }
}
