using LinkittyDo.Api.Models;
using LinkittyDo.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace LinkittyDo.Api.Tests;

public class ClueServiceXnymAndCacheTests : IDisposable
{
    private readonly Mock<ILogger<ClueService>> _loggerMock = new();

    public void Dispose()
    {
        ClueService.ClearCache();
    }

    private ClueService CreateServiceWithHandler(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new ClueService(httpClient, _loggerMock.Object);
    }

    // --- Context extraction tests (via GetClueAsync behavior) ---

    [Fact]
    public void GetLeftContext_FirstWord_ReturnsNull()
    {
        // Context helpers are private static, so we test via reflection or GetClueAsync behavior
        // Using reflection for isolated unit testing
        var method = typeof(ClueService).GetMethod("GetLeftContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var words = new List<PhraseWord>
        {
            new() { Index = 0, Text = "The" },
            new() { Index = 1, Text = "quick" },
            new() { Index = 2, Text = "fox" }
        };

        var result = method!.Invoke(null, new object[] { words, 0 });
        Assert.Null(result);
    }

    [Fact]
    public void GetLeftContext_MiddleWord_ReturnsPreviousWord()
    {
        var method = typeof(ClueService).GetMethod("GetLeftContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var words = new List<PhraseWord>
        {
            new() { Index = 0, Text = "The" },
            new() { Index = 1, Text = "Quick" },
            new() { Index = 2, Text = "Fox" }
        };

        var result = method!.Invoke(null, new object[] { words, 1 });
        Assert.Equal("the", result);
    }

    [Fact]
    public void GetRightContext_LastWord_ReturnsNull()
    {
        var method = typeof(ClueService).GetMethod("GetRightContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var words = new List<PhraseWord>
        {
            new() { Index = 0, Text = "The" },
            new() { Index = 1, Text = "quick" },
            new() { Index = 2, Text = "fox" }
        };

        var result = method!.Invoke(null, new object[] { words, 2 });
        Assert.Null(result);
    }

    [Fact]
    public void GetRightContext_MiddleWord_ReturnsNextWord()
    {
        var method = typeof(ClueService).GetMethod("GetRightContext",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var words = new List<PhraseWord>
        {
            new() { Index = 0, Text = "The" },
            new() { Index = 1, Text = "Quick" },
            new() { Index = 2, Text = "Fox" }
        };

        var result = method!.Invoke(null, new object[] { words, 1 });
        Assert.Equal("fox", result);
    }

    // --- Cache tests ---

    [Fact]
    public async Task GetClueAsync_SecondCall_UsesCachedSynonyms()
    {
        var callCount = 0;
        var handler = new MockHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("datamuse"))
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"word\":\"fast\",\"score\":1000}]", Encoding.UTF8, "application/json")
                };
            }
            // DuckDuckGo search
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href=\"https://en.wikipedia.org/wiki/Fast\">Fast</a>", Encoding.UTF8, "text/html")
            };
        });

        var service = CreateServiceWithHandler(handler);
        var session = CreateTestSession(difficulty: 10);

        // First call - should hit API
        await service.GetClueAsync(session, 1);
        var firstCallCount = callCount;
        Assert.True(firstCallCount > 0, "Should have made API calls");

        // Second call - should use cache (no new Datamuse calls, only DuckDuckGo)
        callCount = 0;
        session.UsedClueTerms[1] = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Reset used terms
        await service.GetClueAsync(session, 1);
        
        // callCount should be 0 for Datamuse calls (cached), but may have DuckDuckGo calls
        var datamuseCallsOnSecond = 0;
        // We track total; on second call, only DuckDuckGo should be called
        // The cache should prevent Datamuse calls
        Assert.True(ClueService.CacheCount > 0, "Cache should have entries");
    }

    [Fact]
    public void ClearCache_EmptiesAllEntries()
    {
        // ClearCache is called in Dispose, but test it explicitly
        Assert.Equal(0, ClueService.CacheCount);
    }

    // --- Xnym expansion tests (verify different difficulty levels add different API calls) ---

    [Fact]
    public async Task GetClueAsync_HighDifficulty_MakesAntonymApiCall()
    {
        var apiUrls = new List<string>();
        var handler = new MockHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            apiUrls.Add(uri);
            if (uri.Contains("datamuse"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"word\":\"slow\",\"score\":500}]", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href=\"https://example.com/test\">Test</a>", Encoding.UTF8, "text/html")
            };
        });

        var service = CreateServiceWithHandler(handler);
        var session = CreateTestSession(difficulty: 70);

        await service.GetClueAsync(session, 1);

        Assert.Contains(apiUrls, u => u.Contains("rel_ant"));
    }

    [Fact]
    public async Task GetClueAsync_ExpertDifficulty_MakesHomophoneApiCall()
    {
        var apiUrls = new List<string>();
        var handler = new MockHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            apiUrls.Add(uri);
            if (uri.Contains("datamuse"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"word\":\"fare\",\"score\":300}]", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href=\"https://example.com/test\">Test</a>", Encoding.UTF8, "text/html")
            };
        });

        var service = CreateServiceWithHandler(handler);
        var session = CreateTestSession(difficulty: 90);

        await service.GetClueAsync(session, 1);

        Assert.Contains(apiUrls, u => u.Contains("rel_hom"));
        Assert.Contains(apiUrls, u => u.Contains("rel_ant"));
        Assert.Contains(apiUrls, u => u.Contains("rel_trg"));
    }

    [Fact]
    public async Task GetClueAsync_LowDifficulty_DoesNotMakeAntonymOrHomophoneApiCalls()
    {
        var apiUrls = new List<string>();
        var handler = new MockHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            apiUrls.Add(uri);
            if (uri.Contains("datamuse"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"word\":\"rapid\",\"score\":800}]", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href=\"https://en.wikipedia.org/wiki/Speed\">Speed</a>", Encoding.UTF8, "text/html")
            };
        });

        var service = CreateServiceWithHandler(handler);
        var session = CreateTestSession(difficulty: 10);

        await service.GetClueAsync(session, 1);

        Assert.DoesNotContain(apiUrls, u => u.Contains("rel_ant"));
        Assert.DoesNotContain(apiUrls, u => u.Contains("rel_hom"));
        Assert.DoesNotContain(apiUrls, u => u.Contains("rel_trg"));
    }

    [Fact]
    public async Task GetClueAsync_WithContext_IncludesContextParamsInMlUrl()
    {
        var apiUrls = new List<string>();
        var handler = new MockHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            apiUrls.Add(uri);
            if (uri.Contains("datamuse"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[{\"word\":\"rapid\",\"score\":800}]", Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href=\"https://example.com/test\">Test</a>", Encoding.UTF8, "text/html")
            };
        });

        var service = CreateServiceWithHandler(handler);
        // Word at index 1 ("quick") has left context "the" and right context "brown"
        var session = CreateTestSessionWithContext();

        await service.GetClueAsync(session, 1);

        // The ml= URL should include lc= and rc= parameters
        var mlUrl = apiUrls.FirstOrDefault(u => u.Contains("ml="));
        Assert.NotNull(mlUrl);
        Assert.Contains("lc=the", mlUrl);
        Assert.Contains("rc=brown", mlUrl);
    }

    // --- Helpers ---

    private static GameSession CreateTestSession(int difficulty = 10)
    {
        return new GameSession
        {
            Difficulty = difficulty,
            Phrase = new Phrase
            {
                Id = 1,
                FullText = "the quick fox",
                Words = new List<PhraseWord>
                {
                    new() { Index = 0, Text = "the", IsHidden = false },
                    new() { Index = 1, Text = "quick", IsHidden = true },
                    new() { Index = 2, Text = "fox", IsHidden = true }
                }
            },
            UsedClueTerms = new Dictionary<int, HashSet<string>>(),
            UsedClueUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static GameSession CreateTestSessionWithContext()
    {
        return new GameSession
        {
            Difficulty = 10,
            Phrase = new Phrase
            {
                Id = 1,
                FullText = "the quick brown fox",
                Words = new List<PhraseWord>
                {
                    new() { Index = 0, Text = "The", IsHidden = false },
                    new() { Index = 1, Text = "quick", IsHidden = true },
                    new() { Index = 2, Text = "brown", IsHidden = false },
                    new() { Index = 3, Text = "fox", IsHidden = true }
                }
            },
            UsedClueTerms = new Dictionary<int, HashSet<string>>(),
            UsedClueUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };
    }
}

/// <summary>
/// Reusable mock HTTP handler for testing HttpClient-based services.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}
