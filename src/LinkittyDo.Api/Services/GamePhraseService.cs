using System.Linq;
using LinkittyDo.Api.Data;
using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Service for managing game phrases and phrase selection logic.
/// Handles phrase retrieval, LLM generation, and ensuring unique gameplay.
/// </summary>
public class GamePhraseService : IGamePhraseService
{
    private readonly IGamePhraseRepository _phraseRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILlmService _llmService;
    private readonly ILogger<GamePhraseService> _logger;
    private readonly Random _random = new();
    
    // Maximum attempts to generate a unique phrase via LLM
    private const int MaxLlmGenerationAttempts = 10;
    
    // Common stop words that should never be hidden for guessing
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Articles
        "a", "an", "the",
        // Pronouns
        "i", "me", "my", "myself", "we", "our", "ours", "ourselves",
        "you", "your", "yours", "yourself", "yourselves",
        "he", "him", "his", "himself", "she", "her", "hers", "herself",
        "it", "its", "itself", "they", "them", "their", "theirs", "themselves",
        "what", "which", "who", "whom", "this", "that", "these", "those",
        // Prepositions
        "in", "on", "at", "by", "for", "with", "about", "against", "between",
        "into", "through", "during", "before", "after", "above", "below",
        "to", "from", "up", "down", "out", "off", "over", "under",
        // Conjunctions
        "and", "but", "or", "nor", "so", "yet", "both", "either", "neither",
        // Auxiliary/Modal verbs
        "is", "am", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "having", "do", "does", "did", "doing",
        "will", "would", "shall", "should", "may", "might", "must", "can", "could",
        // Other common words
        "of", "as", "if", "than", "then", "because", "while", "although",
        "where", "when", "how", "why", "all", "each", "every", "any", "some",
        "no", "not", "only", "own", "same", "just", "also", "very", "too"
    };

    public GamePhraseService(
        IGamePhraseRepository phraseRepository,
        IUserRepository userRepository,
        ILlmService llmService,
        ILogger<GamePhraseService> logger)
    {
        _phraseRepository = phraseRepository;
        _userRepository = userRepository;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<Phrase> GetPhraseForUserAsync(string? userId)
    {
        _logger.LogInformation("Getting phrase for user: {UserId}", userId ?? "guest");

        // Get phrases the user has already played
        var playedPhraseTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                foreach (var game in user.Games)
                {
                    playedPhraseTexts.Add(NormalizeText(game.PhraseText));
                }
                _logger.LogInformation("User has played {Count} phrases", playedPhraseTexts.Count);
            }
        }

        // Get all phrases from the manager
        var allPhrases = (await _phraseRepository.GetAllAsync()).ToList();
        _logger.LogInformation("Phrase manager contains {Count} phrases", allPhrases.Count);

        // Find phrases the user hasn't played yet
        var unplayedPhrases = allPhrases
            .Where(p => !playedPhraseTexts.Contains(NormalizeText(p.Text)))
            .ToList();

        if (unplayedPhrases.Count > 0)
        {
            // Pick a random unplayed phrase
            var selectedPhrase = unplayedPhrases[_random.Next(unplayedPhrases.Count)];
            _logger.LogInformation("Selected existing unplayed phrase: {PhraseId}", selectedPhrase.UniqueId);
            return CreatePhraseFromGamePhrase(selectedPhrase);
        }

        // No unplayed phrases available - need to generate new ones with LLM
        _logger.LogInformation("No unplayed phrases available, generating new phrase with LLM");
        
        var existingPhraseTexts = allPhrases
            .Select(p => NormalizeText(p.Text))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (int attempt = 1; attempt <= MaxLlmGenerationAttempts; attempt++)
        {
            _logger.LogInformation("LLM generation attempt {Attempt}/{MaxAttempts}", attempt, MaxLlmGenerationAttempts);
            
            var generatedPhrase = await GeneratePhraseWithLlmAsync();
            
            if (string.IsNullOrEmpty(generatedPhrase))
            {
                _logger.LogWarning("LLM returned empty phrase, retrying...");
                continue;
            }

            var normalizedGenerated = NormalizeText(generatedPhrase);
            
            // Check if this phrase already exists in the manager
            if (existingPhraseTexts.Contains(normalizedGenerated))
            {
                _logger.LogInformation("Generated phrase already exists in manager: {Phrase}", generatedPhrase);
                continue;
            }

            // Check if user has already played this phrase (shouldn't happen but safety check)
            if (playedPhraseTexts.Contains(normalizedGenerated))
            {
                _logger.LogInformation("Generated phrase was already played by user: {Phrase}", generatedPhrase);
                continue;
            }

            // Add the new phrase to the manager
            var gamePhrase = new GamePhrase
            {
                UniqueId = GamePhrase.GenerateUniqueId(),
                Text = generatedPhrase,
                WordCount = generatedPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                GeneratedByLlm = true,
                CreatedAt = DateTime.UtcNow
            };

            await _phraseRepository.CreateAsync(gamePhrase);
            _logger.LogInformation("Added new LLM-generated phrase to manager: {PhraseId} - {Text}", 
                gamePhrase.UniqueId, gamePhrase.Text);

            return CreatePhraseFromGamePhrase(gamePhrase);
        }

        // If all LLM attempts failed, throw an exception
        _logger.LogError("Failed to generate a unique phrase after {MaxAttempts} attempts", MaxLlmGenerationAttempts);
        throw new InvalidOperationException($"Unable to generate a unique phrase after {MaxLlmGenerationAttempts} attempts");
    }

    public async Task<IEnumerable<GamePhrase>> GetAllPhrasesAsync()
    {
        return await _phraseRepository.GetAllAsync();
    }

    public async Task<int> GetPhraseCountAsync()
    {
        return await _phraseRepository.GetCountAsync();
    }

    /// <summary>
    /// Generates a new phrase using the LLM service
    /// </summary>
    private async Task<string?> GeneratePhraseWithLlmAsync()
    {
        const string systemPrompt = @"You are a phrase generator for a word guessing game. 
Your task is to generate well-known phrases, idioms, proverbs, or common sayings.
Rules:
- The phrase must be 7 words or less
- Use common, well-known phrases that most English speakers would recognize
- Do not include quotes around the phrase
- Return ONLY the phrase, nothing else
- Do not include explanations or context";

        const string userPrompt = @"Generate a single well-known English phrase, idiom, proverb, or common saying that is 7 words or less. 
Examples of good phrases:
- Actions speak louder than words
- Better late than never
- Knowledge is power
- Practice makes perfect
- Time is money

Return only the phrase with no quotes or additional text.";

        try
        {
            var response = await _llmService.GetCompletionAsync(userPrompt, systemPrompt);
            
            if (string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("LLM returned empty content");
                return null;
            }

            // Clean up the response
            var phrase = response.Content
                .Trim()
                .Trim('"', '\'', '.', '!', '?')
                .Trim();

            // Validate word count
            var wordCount = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 7)
            {
                _logger.LogWarning("LLM returned phrase with {WordCount} words (max 7): {Phrase}", wordCount, phrase);
                return null;
            }

            if (wordCount < 2)
            {
                _logger.LogWarning("LLM returned phrase with too few words: {Phrase}", phrase);
                return null;
            }

            _logger.LogInformation("LLM generated phrase: {Phrase} ({WordCount} words)", phrase, wordCount);
            return phrase;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LLM service");
            return null;
        }
    }

    /// <summary>
    /// Creates a Phrase object from a GamePhrase for gameplay
    /// </summary>
    private Phrase CreatePhraseFromGamePhrase(GamePhrase gamePhrase)
    {
        var tokens = TokenizePhrase(gamePhrase.Text);
        
        // Find indices of non-stop words and non-punctuation - these will be hidden for guessing
        var hiddenIndices = tokens
            .Select((token, index) => new { Token = token, Index = index })
            .Where(t => !IsStopWord(t.Token) && !IsPunctuation(t.Token))
            .Select(t => t.Index)
            .ToHashSet();

        // Generate a consistent ID from the phrase's unique ID
        var phraseId = Math.Abs(gamePhrase.UniqueId.GetHashCode());

        return new Phrase
        {
            Id = phraseId,
            FullText = gamePhrase.Text,
            Words = tokens.Select((token, index) => new PhraseWord
            {
                Index = index,
                Text = token,
                IsHidden = hiddenIndices.Contains(index),
                ClueSearchTerm = hiddenIndices.Contains(index) ? token : null
            }).ToList()
        };
    }
    
    /// <summary>
    /// Tokenizes a phrase by separating words from punctuation.
    /// Punctuation marks become their own tokens.
    /// </summary>
    private static List<string> TokenizePhrase(string phrase)
    {
        var tokens = new List<string>();
        var currentWord = new System.Text.StringBuilder();
        
        foreach (var c in phrase)
        {
            if (char.IsWhiteSpace(c))
            {
                // End of word - add if not empty
                if (currentWord.Length > 0)
                {
                    tokens.Add(currentWord.ToString());
                    currentWord.Clear();
                }
            }
            else if (IsPunctuationChar(c))
            {
                // Punctuation found - save current word first, then add punctuation as separate token
                if (currentWord.Length > 0)
                {
                    tokens.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                tokens.Add(c.ToString());
            }
            else
            {
                // Regular character - add to current word
                currentWord.Append(c);
            }
        }
        
        // Don't forget the last word
        if (currentWord.Length > 0)
        {
            tokens.Add(currentWord.ToString());
        }
        
        return tokens;
    }
    
    /// <summary>
    /// Checks if a character is a punctuation mark that should be separated
    /// </summary>
    private static bool IsPunctuationChar(char c)
    {
        // Note: apostrophe (') is NOT included so contractions like "don't" stay together
        return c == ',' || c == '.' || c == '!' || c == '?' || 
               c == ';' || c == ':' || c == '"' ||
               c == '(' || c == ')' || c == '-' || c == '–' || c == '—';
    }
    
    /// <summary>
    /// Checks if a token is punctuation (treated like a stop word - always visible)
    /// </summary>
    private static bool IsPunctuation(string token)
    {
        // Single character punctuation
        if (token.Length == 1 && IsPunctuationChar(token[0]))
            return true;
        
        // Also catch any token that is entirely punctuation
        return token.All(c => IsPunctuationChar(c) || char.IsPunctuation(c));
    }
    
    private static bool IsStopWord(string word)
    {
        return StopWords.Contains(word);
    }
    
    private static string CleanWord(string word)
    {
        return word.Trim(',', '.', '!', '?', ';', ':', '"', '\'');
    }

    private static string NormalizeText(string text)
    {
        return text
            .Trim()
            .ToLowerInvariant()
            .Replace("  ", " ");
    }
}
