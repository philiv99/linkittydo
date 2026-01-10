using LinkittyDo.Api.Models;

namespace LinkittyDo.Api.Services;

public interface IGameService
{
    GameSession StartNewGame();
    GameSession? GetGame(Guid sessionId);
    GuessResponse SubmitGuess(Guid sessionId, GuessRequest request);
    GameState GetGameState(Guid sessionId);
}

public class GameService : IGameService
{
    private readonly Dictionary<Guid, GameSession> _sessions = new();
    private readonly List<Phrase> _phrases;
    private readonly Random _random = new();
    
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

    public GameService()
    {
        // Initialize with sample phrases - words to hide will be auto-selected
        _phrases = new List<Phrase>
        {
            CreatePhrase(1, "The quick brown fox jumps over the lazy dog"),
            CreatePhrase(2, "A penny saved is a penny earned"),
            CreatePhrase(3, "All that glitters is not gold"),
            CreatePhrase(4, "Actions speak louder than words"),
            CreatePhrase(5, "Better late than never"),
            CreatePhrase(6, "Every cloud has a silver lining"),
            CreatePhrase(7, "Knowledge is power"),
            CreatePhrase(8, "Time flies when you are having fun"),
            CreatePhrase(9, "Practice makes perfect"),
            CreatePhrase(10, "Fortune favors the bold"),
        };
    }

    private Phrase CreatePhrase(int id, string text)
    {
        var words = text.Split(' ');
        
        // Find indices of non-stop words - all of these will be hidden for guessing
        var hiddenIndices = words
            .Select((word, index) => new { Word = word, Index = index })
            .Where(w => !IsStopWord(w.Word))
            .Select(w => w.Index)
            .ToHashSet();

        return new Phrase
        {
            Id = id,
            FullText = text,
            Words = words.Select((word, index) => new PhraseWord
            {
                Index = index,
                Text = word,
                IsHidden = hiddenIndices.Contains(index),
                ClueSearchTerm = hiddenIndices.Contains(index) ? CleanWord(word) : null
            }).ToList()
        };
    }
    
    private static bool IsStopWord(string word)
    {
        // Clean punctuation before checking
        var cleanWord = CleanWord(word);
        return StopWords.Contains(cleanWord);
    }
    
    private static string CleanWord(string word)
    {
        // Remove punctuation from start and end
        return word.Trim(',', '.', '!', '?', ';', ':', '"', '\'');
    }

    public GameSession StartNewGame()
    {
        var phrase = _phrases[_random.Next(_phrases.Count)];
        
        var session = new GameSession
        {
            SessionId = Guid.NewGuid(),
            PhraseId = phrase.Id,
            Phrase = phrase,
            RevealedWords = new Dictionary<int, bool>(),
            Score = 0,
            StartedAt = DateTime.UtcNow
        };

        // Initialize all hidden words as not revealed
        foreach (var word in phrase.Words.Where(w => w.IsHidden))
        {
            session.RevealedWords[word.Index] = false;
        }

        _sessions[session.SessionId] = session;
        return session;
    }

    public GameSession? GetGame(Guid sessionId)
    {
        return _sessions.GetValueOrDefault(sessionId);
    }

    public GuessResponse SubmitGuess(Guid sessionId, GuessRequest request)
    {
        var session = GetGame(sessionId);
        if (session == null)
        {
            return new GuessResponse { IsCorrect = false };
        }

        var word = session.Phrase.Words.FirstOrDefault(w => w.Index == request.WordIndex);
        if (word == null || !word.IsHidden)
        {
            return new GuessResponse { IsCorrect = false, CurrentScore = session.Score };
        }

        // Case-insensitive comparison
        var isCorrect = string.Equals(word.Text, request.Guess, StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
        {
            session.RevealedWords[request.WordIndex] = true;
            session.Score += 100; // Base points per correct guess
        }

        var isComplete = session.RevealedWords.All(kv => kv.Value);

        return new GuessResponse
        {
            IsCorrect = isCorrect,
            IsPhraseComplete = isComplete,
            CurrentScore = session.Score,
            RevealedWord = isCorrect ? word.Text : null
        };
    }

    public GameState GetGameState(Guid sessionId)
    {
        var session = GetGame(sessionId);
        if (session == null)
        {
            return new GameState();
        }

        var words = session.Phrase.Words.Select(w => new WordState
        {
            Index = w.Index,
            IsHidden = w.IsHidden,
            IsRevealed = !w.IsHidden || session.RevealedWords.GetValueOrDefault(w.Index, false),
            DisplayText = !w.IsHidden || session.RevealedWords.GetValueOrDefault(w.Index, false) 
                ? w.Text 
                : null
        }).ToList();

        return new GameState
        {
            SessionId = session.SessionId,
            Words = words,
            Score = session.Score,
            IsComplete = session.RevealedWords.All(kv => kv.Value)
        };
    }
}
