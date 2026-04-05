# 06 — Data Models & Storage

## Entity Relationship Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     ENTITY RELATIONSHIPS                        │
│                                                                 │
│  ┌──────────┐        ┌──────────────┐        ┌──────────────┐  │
│  │   User   │ 1───* │  GameRecord  │ *───1  │  GamePhrase  │  │
│  │          │        │              │        │  (phrase     │  │
│  │ USR-...  │        │  GAME-...    │        │   manager)   │  │
│  └──────────┘        └──────┬───────┘        │  PHR-...     │  │
│                             │                └──────────────┘  │
│                             │ 1                                │
│                             │                                  │
│                             │ *                                │
│                      ┌──────┴───────┐                          │
│                      │  GameEvent   │                          │
│                      │  (polymorphic)│                          │
│                      ├──────────────┤                          │
│                      │ ClueEvent    │                          │
│                      │ GuessEvent   │                          │
│                      │ GameEndEvent │                          │
│                      └──────────────┘                          │
│                                                                 │
│  ┌──────────────────────────────┐                               │
│  │      GameSession             │  (in-memory only)             │
│  │      (active play state)     │                               │
│  │                              │                               │
│  │  Contains: Phrase, revealed  │                               │
│  │  words, score, clue tracking │                               │
│  └──────────────────────────────┘                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Domain Models

### User

```csharp
public class User
{
    public string UniqueId { get; set; }         // USR-{unix_ms}-{6_hex}
    public string Name { get; set; }             // 2-50 chars, unique
    public string Email { get; set; }            // Valid email, unique
    public int LifetimePoints { get; set; }      // Cumulative score
    public int PreferredDifficulty { get; set; } // 0-100, default 10
    public List<GameRecord> Games { get; set; }  // Game history
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### Validation Rules

| Field | Rule | Error Code |
|-------|------|-----------|
| Name | Required, 2-50 chars, `[a-zA-Z0-9 _-]` | `VALIDATION_ERROR` |
| Name | Unique (case-insensitive) | `NAME_TAKEN` |
| Email | Required, valid email format | `INVALID_EMAIL` |
| Email | Unique (case-insensitive) | `EMAIL_TAKEN` |
| PreferredDifficulty | 0-100 | `INVALID_DIFFICULTY` |
| Points | Non-negative | `INVALID_POINTS` |

#### Sample JSON (stored file)

```json
{
  "uniqueId": "USR-1736588400000-A1B2C3",
  "name": "PlayerOne",
  "email": "player@example.com",
  "lifetimePoints": 1500,
  "preferredDifficulty": 25,
  "games": [
    {
      "gameId": "GAME-1736590000000-D4E5F6",
      "playedAt": "2026-01-11T13:00:00Z",
      "completedAt": "2026-01-11T13:05:32Z",
      "score": 300,
      "phraseId": 12345,
      "phraseText": "Actions speak louder than words",
      "difficulty": 25,
      "result": "Solved",
      "events": [...]
    }
  ],
  "createdAt": "2026-01-11T12:00:00Z",
  "updatedAt": "2026-01-11T13:05:32Z"
}
```

---

### GamePhrase

Stored in the phrase manager — the system's phrase library.

```csharp
public class GamePhrase
{
    public string UniqueId { get; set; }    // PHR-{unix_ms}-{6_hex}
    public string Text { get; set; }        // "A blessing in disguise"
    public int WordCount { get; set; }      // 4
    public bool GeneratedByLlm { get; set; } // true
    public DateTime CreatedAt { get; set; }
}
```

#### Sample JSON (stored file)

```json
{
  "uniqueId": "PHR-1768156879293-99FDA7",
  "text": "A blessing in disguise",
  "wordCount": 4,
  "generatedByLlm": true,
  "createdAt": "2026-01-11T18:41:19.293Z"
}
```

---

### Phrase (Runtime Model)

Constructed from `GamePhrase` at game start. Contains per-word decomposition with hide/show flags.

```csharp
public class Phrase
{
    public int Id { get; set; }               // Hash of UniqueId
    public string FullText { get; set; }      // "A blessing in disguise"
    public List<PhraseWord> Words { get; set; }
}

public class PhraseWord
{
    public int Index { get; set; }            // Position in token list
    public string Text { get; set; }          // "blessing"
    public bool IsHidden { get; set; }        // true (content word)
    public string? ClueSearchTerm { get; set; } // "blessing" (for clue API)
}
```

#### Phrase Decomposition Example

```
Input:   "A blessing in disguise!"

Tokens:  ["A", "blessing", "in", "disguise", "!"]

Classification:
  Index 0: "A"        → stop word     → IsHidden: false
  Index 1: "blessing" → content word  → IsHidden: true
  Index 2: "in"       → stop word     → IsHidden: false
  Index 3: "disguise" → content word  → IsHidden: true
  Index 4: "!"        → punctuation   → IsHidden: false

Player sees: A ________ in ________ !
```

---

### GameRecord

Persisted within a User's `Games` list when a game completes.

```csharp
public class GameRecord
{
    public string GameId { get; set; }         // GAME-{unix_ms}-{6_hex}
    public DateTime PlayedAt { get; set; }     // Game start time
    public DateTime? CompletedAt { get; set; } // Game end time
    public int Score { get; set; }             // Final score
    public int PhraseId { get; set; }          // Phrase hash ID
    public string PhraseText { get; set; }     // Full phrase text
    public int Difficulty { get; set; }        // Difficulty at game time
    public GameResult Result { get; set; }     // InProgress | Solved | GaveUp
    public List<GameEvent> Events { get; set; } // Ordered event history
}
```

---

### GameEvent Hierarchy (Polymorphic)

```
GameEvent (abstract)
│
├── ClueEvent
│   ├── WordIndex: int
│   ├── SearchTerm: string     ← The synonym used
│   ├── Url: string            ← The URL shown to player
│   └── Timestamp: DateTime
│
├── GuessEvent
│   ├── WordIndex: int
│   ├── GuessText: string      ← What the player typed
│   ├── IsCorrect: bool
│   ├── PointsAwarded: int     ← 100 or 0
│   └── Timestamp: DateTime
│
└── GameEndEvent
    ├── Reason: string          ← "solved" or "gaveup"
    └── Timestamp: DateTime
```

#### JSON Serialization (Polymorphic)

Uses `System.Text.Json` discriminator:

```json
{
  "events": [
    {
      "$type": "clue",
      "wordIndex": 1,
      "searchTerm": "articulate",
      "url": "https://www.merriam-webster.com/dictionary/articulate",
      "timestamp": "2026-01-11T13:00:05Z"
    },
    {
      "$type": "guess",
      "wordIndex": 1,
      "guessText": "speak",
      "isCorrect": true,
      "pointsAwarded": 100,
      "timestamp": "2026-01-11T13:00:25Z"
    },
    {
      "$type": "gameend",
      "reason": "solved",
      "timestamp": "2026-01-11T13:05:32Z"
    }
  ]
}
```

---

### GameSession (In-Memory Runtime)

Not persisted. Exists only while a game is active.

```csharp
public class GameSession
{
    public Guid SessionId { get; set; }
    public int PhraseId { get; set; }
    public Phrase Phrase { get; set; }
    public Dictionary<int, bool> RevealedWords { get; set; }
    public int Score { get; set; }
    public DateTime StartedAt { get; set; }
    public string? UserId { get; set; }
    public GameRecord? GameRecord { get; set; }
    public Dictionary<int, HashSet<string>> UsedClueTerms { get; set; }
    public HashSet<string> UsedClueUrls { get; set; }
    
    public bool IsGuestSession => string.IsNullOrEmpty(UserId);
}
```

---

### Request/Response DTOs

#### StartGameRequest

```csharp
public class StartGameRequest
{
    public string? UserId { get; set; }  // null = guest mode
    public int Difficulty { get; set; } = 10;
}
```

#### GuessRequest / GuessResponse

```csharp
public class GuessRequest
{
    public int WordIndex { get; set; }
    public string Guess { get; set; }
}

public class GuessResponse
{
    public bool IsCorrect { get; set; }
    public bool IsPhraseComplete { get; set; }
    public int CurrentScore { get; set; }
    public string? RevealedWord { get; set; }  // Only set when correct
}
```

#### ClueResponse

```csharp
public class ClueResponse
{
    public string Url { get; set; }
    public string SearchTerm { get; set; }  // The synonym used
}
```

---

## Storage Architecture

### File System Layout

```
Data/
├── Users/
│   ├── USR-1736588400000-A1B2C3.json    ← One file per user
│   ├── USR-1736589200000-D4E5F6.json       (includes game history)
│   └── ...
│
└── Phrases/
    ├── PHR-1768156879293-99FDA7.json    ← One file per phrase
    ├── PHR-1768156931675-2F2176.json       (from LLM or manual)
    └── ...
```

### Concurrency Model

```
SemaphoreSlim(1, 1) — ensures single-writer access

  Read:   await _lock.WaitAsync()
          try { read all files } finally { _lock.Release() }

  Write:  await _lock.WaitAsync()
          try { serialize + write file } finally { _lock.Release() }

  Batch:  await _lock.WaitAsync()
          try { write all files in loop } finally { _lock.Release() }
```

### Data Integrity

| Mechanism | Protection |
|-----------|-----------|
| SemaphoreSlim | Concurrent write prevention |
| JSON serialization | WriteIndented for human readability |
| File-per-entity | Corruption isolation (one bad file ≠ all data lost) |
| CamelCase naming | Consistent JSON property casing |

---

## ID Generation

All entity IDs follow a consistent pattern:

```
{PREFIX}-{UNIX_TIMESTAMP_MS}-{RANDOM_6_HEX}

  PREFIX:     USR / PHR / GAME
  TIMESTAMP:  13-digit Unix timestamp in milliseconds
  RANDOM:     6 uppercase hex characters from Guid

  Collision probability:
    Same millisecond: ~1 in 16^6 = ~1 in 16.7 million
    Different millisecond: 0
```

### Generation Code

```csharp
public static string GenerateUniqueId()
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var random = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
    return $"PREFIX-{timestamp}-{random}";
}
```

---

## Migration Path: JSON → Database

The repository pattern enables zero-service-code migration:

```
Step 1: Implement new repository
  class SqlUserRepository : IUserRepository { ... }

Step 2: Change DI registration
  // builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();
  builder.Services.AddScoped<IUserRepository, SqlUserRepository>();

Step 3: Migrate data
  Read all JSON files → Insert into database tables

Step 4: Delete JSON files (after verification)
```

### Suggested Schema

```sql
CREATE TABLE Users (
    UniqueId        VARCHAR(30) PRIMARY KEY,
    Name            VARCHAR(50) NOT NULL UNIQUE,
    Email           VARCHAR(255) NOT NULL UNIQUE,
    LifetimePoints  INT DEFAULT 0,
    PreferredDifficulty INT DEFAULT 10,
    CreatedAt       DATETIME NOT NULL,
    UpdatedAt       DATETIME
);

CREATE TABLE GameRecords (
    GameId      VARCHAR(30) PRIMARY KEY,
    UserId      VARCHAR(30) REFERENCES Users(UniqueId),
    PlayedAt    DATETIME NOT NULL,
    CompletedAt DATETIME,
    Score       INT DEFAULT 0,
    PhraseId    INT,
    PhraseText  VARCHAR(200),
    Difficulty  INT,
    Result      VARCHAR(20)
);

CREATE TABLE GameEvents (
    Id          INT IDENTITY PRIMARY KEY,
    GameId      VARCHAR(30) REFERENCES GameRecords(GameId),
    EventType   VARCHAR(10),  -- 'clue', 'guess', 'gameend'
    WordIndex   INT,
    SearchTerm  VARCHAR(100),
    Url         VARCHAR(2000),
    GuessText   VARCHAR(100),
    IsCorrect   BIT,
    PointsAwarded INT,
    Reason      VARCHAR(20),
    Timestamp   DATETIME
);

CREATE TABLE GamePhrases (
    UniqueId      VARCHAR(30) PRIMARY KEY,
    Text          VARCHAR(200) NOT NULL UNIQUE,
    WordCount     INT,
    GeneratedByLlm BIT DEFAULT 0,
    CreatedAt     DATETIME NOT NULL
);
```

---

*Next: [07 — Analytics & Reinforcement Learning](07-analytics-and-learning.md)*
