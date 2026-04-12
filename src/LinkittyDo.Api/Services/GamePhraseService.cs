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
    private readonly IGameRecordRepository _gameRecordRepository;
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
        IGameRecordRepository gameRecordRepository,
        ILlmService llmService,
        ILogger<GamePhraseService> logger)
    {
        _phraseRepository = phraseRepository;
        _userRepository = userRepository;
        _gameRecordRepository = gameRecordRepository;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<Phrase> GetPhraseForUserAsync(string? userId, int preferredDifficulty = 10)
    {
        _logger.LogInformation("Getting phrase for user: {UserId}, preferred difficulty: {Difficulty}", 
            userId ?? "guest", preferredDifficulty);

        // Get phrases the user has already played
        var playedPhraseTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                var games = await _gameRecordRepository.GetByUserIdAsync(userId, 1, int.MaxValue);
                foreach (var game in games)
                {
                    playedPhraseTexts.Add(NormalizeText(game.PhraseText));
                }
                _logger.LogInformation("User has played {Count} phrases", playedPhraseTexts.Count);
            }
        }

        // Get all phrases from the manager
        var allPhrases = (await _phraseRepository.GetAllAsync()).ToList();
        _logger.LogInformation("Phrase manager contains {Count} phrases", allPhrases.Count);

        // Build a set of all existing phrase texts for quick lookup
        var existingPhraseTexts = allPhrases
            .Select(p => NormalizeText(p.Text))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Find phrases the user hasn't played yet
        var unplayedPhrases = allPhrases
            .Where(p => !playedPhraseTexts.Contains(NormalizeText(p.Text)))
            .ToList();

        if (unplayedPhrases.Count > 0)
        {
            // Ensure all phrases have difficulty computed
            foreach (var phrase in unplayedPhrases.Where(p => p.Difficulty == 0))
            {
                var tokens = TokenizePhrase(phrase.Text);
                var hiddenIndices = tokens
                    .Select((token, index) => new { Token = token, Index = index })
                    .Where(t => !IsStopWord(t.Token) && !IsPunctuation(t.Token))
                    .Select(t => t.Index)
                    .ToHashSet();
                phrase.Difficulty = ComputePhraseDifficulty(tokens, hiddenIndices);
            }
            
            // Prefer phrases within ±20 of preferred difficulty
            var nearDifficulty = unplayedPhrases
                .Where(p => Math.Abs(p.Difficulty - preferredDifficulty) <= 20)
                .ToList();
            
            var selectionPool = nearDifficulty.Count > 0 ? nearDifficulty : unplayedPhrases;
            var selectedPhrase = selectionPool[_random.Next(selectionPool.Count)];
            _logger.LogInformation("Selected existing unplayed phrase: {PhraseId} (difficulty: {Difficulty})", 
                selectedPhrase.UniqueId, selectedPhrase.Difficulty);
            return CreatePhraseFromGamePhrase(selectedPhrase);
        }

        // No unplayed phrases available - need to generate new ones with LLM
        _logger.LogInformation("No unplayed phrases available, generating batch of phrases with LLM");
        
        // Generate 10 phrases at once using the batch generation method
        var newPhrasesAdded = await GenerateAndAddNewPhrasesAsync(existingPhraseTexts);

        if (newPhrasesAdded.Count > 0)
        {
            // Filter to phrases not played by this user (should be all of them since they're new)
            var unplayedNewPhrases = newPhrasesAdded
                .Where(p => !playedPhraseTexts.Contains(NormalizeText(p.Text)))
                .ToList();

            if (unplayedNewPhrases.Count > 0)
            {
                // Return a random one of the newly added phrases
                var selectedPhrase = unplayedNewPhrases[_random.Next(unplayedNewPhrases.Count)];
                _logger.LogInformation("Selected newly generated phrase: {PhraseId} - {Text}", 
                    selectedPhrase.UniqueId, selectedPhrase.Text);
                return CreatePhraseFromGamePhrase(selectedPhrase);
            }
        }

        // If batch generation failed, fall back to single phrase generation with retries
        _logger.LogWarning("Batch generation returned no new phrases, falling back to single phrase generation");
        
        return await GenerateSinglePhraseWithRetriesAsync(existingPhraseTexts, playedPhraseTexts);
    }

    /// <summary>
    /// Generates a batch of phrases via LLM and adds new unique ones to the game manager.
    /// Returns the list of newly added GamePhrase objects.
    /// </summary>
    private async Task<List<GamePhrase>> GenerateAndAddNewPhrasesAsync(HashSet<string> existingPhraseTexts)
    {
        _logger.LogInformation("Generating batch of 10 phrases via LLM");
        
        var generatedPhrases = await _llmService.GeneratePhrasesAsync(10);
        
        if (generatedPhrases.Count == 0)
        {
            _logger.LogWarning("LLM returned no phrases");
            return new List<GamePhrase>();
        }

        _logger.LogInformation("LLM returned {Count} phrases, checking for new ones", generatedPhrases.Count);

        var newPhrases = new List<GamePhrase>();
        
        foreach (var phraseText in generatedPhrases)
        {
            var normalizedText = NormalizeText(phraseText);
            
            // Check if this phrase already exists in the manager
            if (existingPhraseTexts.Contains(normalizedText))
            {
                _logger.LogDebug("Phrase already exists in manager, skipping: {Phrase}", phraseText);
                continue;
            }

            // This is a new phrase - create a GamePhrase for it
            var gamePhrase = new GamePhrase
            {
                UniqueId = GamePhrase.GenerateUniqueId(),
                Text = phraseText,
                WordCount = phraseText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                GeneratedByLlm = true,
                CreatedAt = DateTime.UtcNow
            };

            newPhrases.Add(gamePhrase);
            
            // Add to our tracking set so we don't add duplicates from the same batch
            existingPhraseTexts.Add(normalizedText);
            
            _logger.LogInformation("Found new phrase to add: {Text}", phraseText);
        }

        if (newPhrases.Count > 0)
        {
            // Batch add all new phrases to the repository
            await _phraseRepository.CreateManyAsync(newPhrases);
            _logger.LogInformation("Added {Count} new phrases to the game manager", newPhrases.Count);
        }
        else
        {
            _logger.LogWarning("All {Count} generated phrases already exist in the manager", generatedPhrases.Count);
        }

        return newPhrases;
    }

    /// <summary>
    /// Fallback method: Generates a single phrase with multiple retry attempts
    /// </summary>
    private async Task<Phrase> GenerateSinglePhraseWithRetriesAsync(
        HashSet<string> existingPhraseTexts, 
        HashSet<string> playedPhraseTexts)
    {
        for (int attempt = 1; attempt <= MaxLlmGenerationAttempts; attempt++)
        {
            _logger.LogInformation("Single phrase generation attempt {Attempt}/{MaxAttempts}", 
                attempt, MaxLlmGenerationAttempts);
            
            var generatedPhrase = await GenerateSinglePhraseWithLlmAsync();
            
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

    public async Task<Phrase?> GetPhraseByUniqueIdAsync(string uniqueId)
    {
        var gamePhrase = await _phraseRepository.GetByIdAsync(uniqueId);
        if (gamePhrase == null || !gamePhrase.IsActive)
            return null;
        return CreatePhraseFromGamePhrase(gamePhrase);
    }

    /// <summary>
    /// Generates a single new phrase using the LLM service (used as fallback)
    /// </summary>
    private async Task<string?> GenerateSinglePhraseWithLlmAsync()
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

        // Compute difficulty if not already set
        if (gamePhrase.Difficulty == 0)
        {
            gamePhrase.Difficulty = ComputePhraseDifficulty(tokens, hiddenIndices);
        }

        // Generate a consistent ID from the phrase's unique ID
        var phraseId = Math.Abs(gamePhrase.UniqueId.GetHashCode());

        return new Phrase
        {
            Id = phraseId,
            UniqueId = gamePhrase.UniqueId,
            FullText = gamePhrase.Text,
            Difficulty = gamePhrase.Difficulty,
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
    /// Computes a difficulty score (0-100) for a phrase based on:
    /// - Hidden word ratio (more hidden = harder)
    /// - Phrase length (longer = harder)
    /// - Average hidden word length (longer words = harder)
    /// </summary>
    internal static int ComputePhraseDifficulty(List<string> tokens, HashSet<int> hiddenIndices)
    {
        var contentTokens = tokens.Where((t, i) => !IsPunctuation(t)).ToList();
        if (contentTokens.Count == 0) return 50;

        // Factor 1: Hidden word ratio (0-1), higher = harder
        var hiddenRatio = (double)hiddenIndices.Count / contentTokens.Count;
        
        // Factor 2: Phrase length score (0-1), longer = harder
        // 2 words = 0.0, 7+ words = 1.0
        var lengthScore = Math.Min(1.0, Math.Max(0.0, (contentTokens.Count - 2.0) / 5.0));
        
        // Factor 3: Average hidden word length score (0-1), longer words = harder
        var hiddenWords = tokens.Where((t, i) => hiddenIndices.Contains(i)).ToList();
        var avgWordLength = hiddenWords.Count > 0 
            ? hiddenWords.Average(w => w.Length) 
            : 3.0;
        // 3 chars = 0.0, 8+ chars = 1.0
        var wordLengthScore = Math.Min(1.0, Math.Max(0.0, (avgWordLength - 3.0) / 5.0));
        
        // Weighted combination
        var rawScore = (hiddenRatio * 40) + (lengthScore * 30) + (wordLengthScore * 30);
        
        return (int)Math.Round(Math.Min(100, Math.Max(0, rawScore)));
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

    /// <summary>
    /// Computes difficulty from raw phrase text using the same algorithm as phrase selection.
    /// Useful for seeding phrases with pre-computed difficulty values.
    /// </summary>
    public static int ComputeDifficultyFromText(string phraseText)
    {
        var tokens = TokenizePhrase(phraseText);
        var hiddenIndices = new HashSet<int>();
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!IsPunctuation(tokens[i]) && !IsStopWord(tokens[i]))
                hiddenIndices.Add(i);
        }
        return ComputePhraseDifficulty(tokens, hiddenIndices);
    }
}
