# LinkittyDo - Application Design Document

## Overview

LinkittyDo is a word-guessing game where players attempt to reveal a hidden phrase by guessing individual words. Each hidden word can be investigated using clue buttons that open search results in new browser tabs, allowing players to research while keeping the game visible.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Browser                                │
│  ┌─────────────────┬─────────┬─────────┬─────────┐         │
│  │  Game Tab       │ Clue 1  │ Clue 2  │ Clue 3  │  ...    │
│  │  (React App)    │  (URL)  │  (URL)  │  (URL)  │         │
│  └─────────────────┴─────────┴─────────┴─────────┘         │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                   .NET Web API                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ Game         │  │ Clue         │  │ Phrase       │      │
│  │ Controller   │  │ Controller   │  │ Controller   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│           │                │                │               │
│           ▼                ▼                ▼               │
│  ┌─────────────────────────────────────────────────┐       │
│  │              Game Service Layer                 │       │
│  └─────────────────────────────────────────────────┘       │
│                          │                                  │
│                          ▼                                  │
│  ┌─────────────────────────────────────────────────┐       │
│  │         Search Engine Integration               │       │
│  │         (Bing/DuckDuckGo API)                   │       │
│  └─────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React.js (with TypeScript) |
| Styling | CSS Modules or Tailwind CSS |
| Backend | .NET 8 Web API (C#) |
| Search Integration | Bing Search API / DuckDuckGo |
| State Management | React useState/useReducer |

---

## Data Models

### Phrase

```csharp
public class Phrase
{
    public int Id { get; set; }
    public string FullText { get; set; }           // "The quick brown fox"
    public List<PhraseWord> Words { get; set; }
}
```

### PhraseWord

```csharp
public class PhraseWord
{
    public int Index { get; set; }                 // Position in phrase
    public string Text { get; set; }               // Actual word
    public bool IsHidden { get; set; }             // Whether to hide this word
    public string? ClueSearchTerm { get; set; }    // Optional custom search term for clue
}
```

### GameSession

```csharp
public class GameSession
{
    public Guid SessionId { get; set; }
    public int PhraseId { get; set; }
    public Dictionary<int, bool> RevealedWords { get; set; }  // Index -> IsRevealed
    public int Score { get; set; }
    public DateTime StartedAt { get; set; }
}
```

### GuessRequest / GuessResponse

```csharp
public class GuessRequest
{
    public Guid SessionId { get; set; }
    public int WordIndex { get; set; }
    public string Guess { get; set; }
}

public class GuessResponse
{
    public bool IsCorrect { get; set; }
    public bool IsPhraseComplete { get; set; }
    public int CurrentScore { get; set; }
}
```

### ClueResponse

```csharp
public class ClueResponse
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Snippet { get; set; }
}
```

---

## API Endpoints

### Game Controller

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/game/start` | Start a new game session |
| GET | `/api/game/{sessionId}` | Get current game state |
| POST | `/api/game/{sessionId}/guess` | Submit a guess for a word |

### Clue Controller

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/clue/{sessionId}/{wordIndex}` | Get clue URL for a hidden word |

### Phrase Controller (Admin)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/phrases` | List available phrases |
| POST | `/api/phrases` | Add a new phrase |
| GET | `/api/phrases/{id}` | Get phrase details |

---

## Frontend Components

```
src/
├── components/
│   ├── Game/
│   │   ├── GameBoard.tsx          # Main game container
│   │   ├── PhraseDisplay.tsx      # Renders the phrase with inputs
│   │   ├── WordSlot.tsx           # Individual word slot (revealed or input)
│   │   ├── GuessInput.tsx         # Text input for guessing
│   │   ├── ClueButton.tsx         # Button to open clue in new tab
│   │   └── ScoreDisplay.tsx       # Shows current score
│   ├── Layout/
│   │   ├── Header.tsx
│   │   └── Footer.tsx
│   └── common/
│       ├── Button.tsx
│       └── Input.tsx
├── hooks/
│   ├── useGame.ts                 # Game state management
│   └── useClue.ts                 # Clue fetching logic
├── services/
│   └── api.ts                     # API client
├── types/
│   └── index.ts                   # TypeScript interfaces
├── App.tsx
└── index.tsx
```

### Key Component: PhraseDisplay

```tsx
// Conceptual structure
<PhraseDisplay>
  <WordSlot word="The" isHidden={false} />
  <WordSlot word="quick" isHidden={true}>
    <GuessInput onGuess={handleGuess} />
    <ClueButton onClick={openClue} />
  </WordSlot>
  <WordSlot word="brown" isHidden={false} />
  <WordSlot word="fox" isHidden={true}>
    <GuessInput onGuess={handleGuess} />
    <ClueButton onClick={openClue} />
  </WordSlot>
</PhraseDisplay>
```

---

## Game Flow

```
1. Player clicks "Start Game"
       │
       ▼
2. Frontend calls POST /api/game/start
       │
       ▼
3. Backend creates GameSession, selects random Phrase
       │
       ▼
4. Frontend displays phrase with hidden words as input boxes
       │
       ▼
5. Player interaction loop:
       │
       ├──► Player types guess in input box
       │         │
       │         ▼
       │    POST /api/game/{sessionId}/guess
       │         │
       │         ├── Correct: Reveal word, update score
       │         └── Incorrect: Show feedback, no change
       │
       └──► Player clicks "Clue" button
                 │
                 ▼
            GET /api/clue/{sessionId}/{wordIndex}
                 │
                 ▼
            window.open(clueUrl, '_blank')
            (Opens in new tab, game tab stays focused)
       │
       ▼
6. All words revealed → Show completion screen with score
```

---

## Clue Generation Strategy

The clue system generates search URLs based on context:

1. **Direct Search**: Use the hidden word + phrase context
2. **Synonym Search**: Search for synonyms/related terms
3. **Definition Search**: Search for definitions

```csharp
public class ClueService
{
    public async Task<ClueResponse> GetClueAsync(string word, string phraseContext)
    {
        // Build search query that hints at the word without revealing it
        var searchQuery = BuildContextualQuery(word, phraseContext);
        
        // Get search result URL from search API
        var searchUrl = await _searchClient.GetTopResultUrl(searchQuery);
        
        return new ClueResponse { Url = searchUrl };
    }
}
```

### Search URL Options

| Provider | URL Pattern |
|----------|-------------|
| DuckDuckGo | `https://duckduckgo.com/?q={query}` |
| Google | `https://www.google.com/search?q={query}` |
| Bing | `https://www.bing.com/search?q={query}` |

For MVP, we can simply construct search URLs directly without API integration.

---

## UI Wireframe

```
┌─────────────────────────────────────────────────────────────┐
│  🐱 LinkittyDo                              Score: 150 pts  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│    The  [_________] 🔍  brown  [_________] 🔍  jumps        │
│              ▲                       ▲                      │
│         Input box              Input box                    │
│         + Clue btn             + Clue btn                   │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                     [ Give Up ]                             │
└─────────────────────────────────────────────────────────────┘

Legend:
- [_________] = Text input for guessing
- 🔍 = Clue button (opens new tab)
```

---

## MVP Scope

### Phase 1 (MVP)
- [ ] Single hardcoded phrase
- [ ] Basic guess validation (case-insensitive)
- [ ] Clue buttons open DuckDuckGo search in new tab
- [ ] Simple scoring (points per correct guess)
- [ ] Win condition when all words revealed

### Phase 2
- [ ] Multiple phrases from database
- [ ] Phrase categories/difficulty levels
- [ ] Improved clue generation with search API
- [ ] Score persistence
- [ ] Timer-based scoring

### Phase 3
- [ ] User accounts
- [ ] Leaderboards
- [ ] Custom phrase creation
- [ ] Multiplayer mode

---

## Project Structure

```
LinkittyDo/
├── src/
│   ├── LinkittyDo.Api/           # .NET Web API project
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Models/
│   │   └── Program.cs
│   └── linkittydo-web/           # React frontend
│       ├── src/
│       ├── public/
│       └── package.json
├── docs/
│   └── design.md                 # This document
└── README.md
```

---

## Next Steps

1. **Set up project structure**
   - Create .NET Web API project
   - Create React app with TypeScript

2. **Implement backend MVP**
   - Game controller with start/guess endpoints
   - Clue endpoint returning search URLs
   - In-memory game state (no database for MVP)

3. **Implement frontend MVP**
   - GameBoard component
   - PhraseDisplay with input slots
   - Clue button functionality

4. **Integration & Testing**
   - Connect frontend to backend
   - Test game flow end-to-end

---

## Open Questions

1. **Search Provider**: Which search engine to use for clues? (DuckDuckGo is privacy-friendly and doesn't require API key for basic searches)

2. **Clue Quality**: Should clues be the search results page, or should we fetch and display a specific result URL?

3. **Word Matching**: How strict should guess matching be? (Exact match, case-insensitive, fuzzy matching?)

4. **Phrase Source**: Where will phrases come from? (Curated list, user submissions, API?)
