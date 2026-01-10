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
    private readonly Random _random = new();

    public ClueService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
        var searchTerm = await GetUnusedSynonymAsync(word.Text, session.UsedClueTerms[wordIndex]);
        
        // Track that we've used this term
        session.UsedClueTerms[wordIndex].Add(searchTerm);

        // Search for actual URLs using the synonym
        var clueUrl = await GetSearchResultUrlAsync(searchTerm, session.UsedClueUrls);
        
        // Track that we've used this URL
        if (!string.IsNullOrEmpty(clueUrl))
        {
            session.UsedClueUrls.Add(clueUrl);
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
            var html = await _httpClient.GetStringAsync(searchUrl);

            // Parse result URLs from DuckDuckGo HTML
            var urls = ParseDuckDuckGoResults(html);

            // Filter out already used URLs and pick a random one
            var availableUrls = urls
                .Where(u => !usedUrls.Contains(u))
                .Where(u => !u.Contains("duckduckgo.com"))
                .Where(u => IsValidClueUrl(u))
                .ToList();

            if (availableUrls.Count > 0)
            {
                return availableUrls[_random.Next(availableUrls.Count)];
            }

            // Fallback: return first available URL even if used
            var fallbackUrl = urls.FirstOrDefault(u => IsValidClueUrl(u));
            return fallbackUrl ?? $"https://en.wikipedia.org/wiki/{HttpUtility.UrlEncode(searchTerm)}";
        }
        catch
        {
            // Fallback to Wikipedia if search fails
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
            
            var availableSynonyms = synonyms
                .Where(s => !s.Equals(word, StringComparison.OrdinalIgnoreCase))
                .Where(s => !usedTerms.Contains(s))
                .ToList();

            if (availableSynonyms.Count > 0)
            {
                return availableSynonyms[_random.Next(availableSynonyms.Count)];
            }

            // Fallback: use the word itself with "meaning" if no synonyms available
            return $"{word} meaning";
        }
        catch
        {
            return $"{word} definition";
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
            var response = await _httpClient.GetStringAsync(url);
            var words = JsonSerializer.Deserialize<List<DatamuseWord>>(response);
            return words?.Select(w => w.word).Where(w => !string.IsNullOrEmpty(w)).ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private class DatamuseWord
    {
        public string word { get; set; } = string.Empty;
        public int score { get; set; }
    }
}
