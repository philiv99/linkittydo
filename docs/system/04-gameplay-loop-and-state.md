# 04 — Gameplay Loop & State Machine

## Game Lifecycle

Every game session moves through a defined lifecycle with clear state transitions.

```
                    ┌─────────────┐
                    │    IDLE     │  No active game
                    └──────┬──────┘
                           │ Player clicks "New Game"
                           │ POST /api/game/start
                           ▼
                    ┌─────────────┐
              ┌────►│  PLAYING    │◄────────────────────┐
              │     └──┬───┬──┬──┘                      │
              │        │   │  │                         │
              │        │   │  └── Get Clue ─────────┐   │
              │        │   │     GET /clue/{sid}/{wi}│   │
              │        │   │                        │   │
              │        │   │  ┌─────────────────────┘   │
              │        │   │  │                         │
              │        │   │  ▼                         │
              │        │   │  Opens URL in new tab      │
              │        │   │  Player reads clue page    │
              │        │   │  Returns to game tab ──────┘
              │        │   │
              │        │   └── Submit Guess
              │        │      POST /game/{sid}/guess
              │        │           │
              │        │     ┌─────┴─────┐
              │        │     │           │
              │        │  Incorrect   Correct
              │        │     │           │
              │        │     │     ┌─────┴──────────┐
              │        │     │     │                │
              │        │     │  Not all words    All words
              │        │     │  revealed yet     revealed!
              │        │     │     │                │
              │        │     └─────┘                │
              │        │           │                ▼
              │        │           │         ┌─────────────┐
              │        │           └────────►│   SOLVED    │
              │        │                     └──────┬──────┘
              │        │                            │
              │        └── Give Up                  │
              │           POST /game/{sid}/give-up   │
              │                │                    │
              │                ▼                    │
              │         ┌─────────────┐             │
              │         │  GAVE UP    │             │
              │         └──────┬──────┘             │
              │                │                    │
              │                └────────┬───────────┘
              │                         │
              │                         ▼
              │                  ┌─────────────┐
              │                  │  COMPLETE   │  Score displayed
              │                  └──────┬──────┘
              │                         │ Player clicks "New Game"
              └─────────────────────────┘
```

---

## State Model

### Server-Side: `GameSession`

The server maintains the authoritative game state:

```
GameSession
├── SessionId: Guid              ← Unique session identifier
├── PhraseId: int                ← Which phrase is being played
├── Phrase: Phrase               ← Full phrase with word list
├── RevealedWords: Dict<int,bool>← Which hidden words have been guessed
├── Score: int                   ← Running point total
├── StartedAt: DateTime          ← Session creation time
├── UserId: string?              ← Null for guest sessions
├── GameRecord: GameRecord?      ← Event log (null for guests)
├── UsedClueTerms: Dict<int, HashSet<string>>  ← Synonym dedup per word
└── UsedClueUrls: HashSet<string>              ← URL dedup across session
```

### Client-Side: `GameState`

The client receives a projection — no answer information is exposed:

```
GameState (sent to client)
├── sessionId: string
├── words: WordState[]
│   ├── index: number
│   ├── displayText: string | null   ← null if still hidden
│   ├── isHidden: boolean            ← true if this is a guessable word
│   └── isRevealed: boolean          ← true if guessed or visible
├── score: number
└── isComplete: boolean
```

**Security note**: The client never receives the answer words for hidden slots. The `displayText` is `null` until the word is correctly guessed. All validation is server-side.

---

## The Execution Loop

### Per-Turn Sequence Diagram

```
  Player          Frontend           Backend            Datamuse      DuckDuckGo
    │                │                  │                   │              │
    │  Click Clue    │                  │                   │              │
    │───────────────►│                  │                   │              │
    │                │  GET /clue/      │                   │              │
    │                │  {sid}/{wIdx}    │                   │              │
    │                │─────────────────►│                   │              │
    │                │                  │  rel_syn + ml     │              │
    │                │                  │──────────────────►│              │
    │                │                  │  [synonym list]   │              │
    │                │                  │◄──────────────────│              │
    │                │                  │  Pick unused syn  │              │
    │                │                  │                   │              │
    │                │                  │  Search synonym   │              │
    │                │                  │──────────────────────────────────►
    │                │                  │  [HTML results]   │              │
    │                │                  │◄──────────────────────────────────
    │                │                  │  Parse URLs       │              │
    │                │                  │  Filter used      │              │
    │                │                  │  Random select    │              │
    │                │  { url, term }   │                   │              │
    │                │◄─────────────────│                   │              │
    │                │                  │                   │              │
    │  Validate URL  │                  │                   │              │
    │◄───────────────│                  │                   │              │
    │                │  (retry if 404)  │                   │              │
    │                │                  │                   │              │
    │  Open in       │                  │                   │              │
    │  new tab       │                  │                   │              │
    │◄───────────────│                  │                   │              │
    │                │                  │                   │              │
    │  [ reads page, reasons about word ]                  │              │
    │                │                  │                   │              │
    │  Type guess    │                  │                   │              │
    │───────────────►│                  │                   │              │
    │                │  POST /guess     │                   │              │
    │                │  {wIdx, guess}   │                   │              │
    │                │─────────────────►│                   │              │
    │                │                  │  Compare guess    │              │
    │                │                  │  (case-insensitive)              │
    │                │  { isCorrect,    │                   │              │
    │                │    score,        │                   │              │
    │                │    revealed? }   │                   │              │
    │                │◄─────────────────│                   │              │
    │  Update UI     │                  │                   │              │
    │◄───────────────│                  │                   │              │
```

### URL Validation & Retry

The frontend validates every clue URL before presenting it:

```
Clue Retrieval with Validation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  for attempt in 1..5:
    │
    ├─ Request clue from API (with excluded URLs)
    │
    ├─ Receive { url, searchTerm }
    │
    ├─ HEAD request to URL (5s timeout, no-cors)
    │    │
    │    ├─ Success → return URL (open in new tab)
    │    │
    │    └─ Failure → add URL to exclusion list
    │                  persist in localStorage
    │                  retry with new exclusion
    │
    └─ After 5 failures → return null (no clue available)
```

---

## Scoring System

### Current Implementation

| Event | Points |
|-------|--------|
| Correct guess | +100 |
| Incorrect guess | +0 |
| Give up | Score reset to 0 |
| Game complete (solved) | Sum of all correct guesses |

### Scoring Formula (Current)

$$\text{Score} = 100 \times |\{w : w \text{ correctly guessed}\}|$$

### Enhanced Scoring (From Original Architecture)

The original design documents describe a more nuanced scoring model:

$$\text{WordScore}(w) = \frac{100}{n_{\text{clues}}(w) \cdot n_{\text{guesses}}(w)}$$

Where:
- $n_{\text{clues}}(w)$ = number of clues requested for word $w$
- $n_{\text{guesses}}(w)$ = number of guess attempts for word $w$

This rewards players who guess correctly with fewer clues and fewer attempts.

### Clue Weight Accumulation (Original Design)

The original architecture describes a **clue weighting** system:

> *"Correct word — apply for each correctly guessed word: Add fractional weight amount to each clue for the given correct word(s) decreasing in proportion based on nth prior clue and number of guesses"*

This means each clue accumulates a weight based on whether it led to a correct guess:

$$w_{\text{clue}_i} = w_{\text{clue}_i} + \frac{\alpha}{i \cdot n_{\text{guesses}}}$$

Where $\alpha$ is a base weight and $i$ is the position of the clue (most recent = 1). This is the foundation for the reinforcement learning system described in [07 Analytics](07-analytics-and-learning.md).

---

## Event Recording

For registered users, every significant action is recorded as a `GameEvent`:

```
Game Event Timeline (example)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  T+0s    GameRecord created (gameId: GAME-1736588400000-A1B2C3)
  T+5s    ClueEvent   { wordIndex: 1, searchTerm: "talk",     url: "https://..." }
  T+12s   GuessEvent  { wordIndex: 1, guess: "say",    isCorrect: false,  pts: 0   }
  T+18s   ClueEvent   { wordIndex: 1, searchTerm: "utter",    url: "https://..." }
  T+25s   GuessEvent  { wordIndex: 1, guess: "speak",  isCorrect: true,   pts: 100 }
  T+30s   ClueEvent   { wordIndex: 4, searchTerm: "noisy",    url: "https://..." }
  T+38s   GuessEvent  { wordIndex: 4, guess: "louder", isCorrect: true,   pts: 100 }
  T+40s   ClueEvent   { wordIndex: 6, searchTerm: "language", url: "https://..." }
  T+52s   GuessEvent  { wordIndex: 6, guess: "words",  isCorrect: true,   pts: 100 }
  T+52s   GameEndEvent { reason: "solved" }
```

### Guest vs Registered Sessions

| Feature | Guest | Registered |
|---------|-------|-----------|
| Play games | ✓ | ✓ |
| Get clues | ✓ | ✓ |
| Submit guesses | ✓ | ✓ |
| Event recording | ✗ | ✓ |
| Game history | ✗ | ✓ |
| Lifetime points | ✗ | ✓ |
| Analytics data | ✗ | ✓ |

---

## Session Lifecycle

```
Session Memory Management
━━━━━━━━━━━━━━━━━━━━━━━━━

  Create:  POST /api/game/start
           → New Guid allocated
           → GameSession added to Dictionary
           → GameRecord created (if registered user)

  Active:  All subsequent requests use sessionId
           → Clue, Guess, State queries
           → Session updated in-place (mutable)

  Complete: All words guessed OR player gives up
           → GameRecord saved to user's game history
           → Session remains in memory (queryable)

  Expire:  (Currently: never — cleared on restart)
           (Future: TTL-based eviction, e.g., 24 hours)
```

---

## Error Handling

### Server-Side Error Responses

| Scenario | HTTP Status | Response |
|----------|-------------|----------|
| Session not found | 404 | `{ message: "Game session not found" }` |
| Word not found | 404 | `{ message: "Word not found" }` |
| Word not hidden | 400 | `{ message: "Word is not hidden, no clue needed" }` |
| Datamuse API failure | 200 | Falls back to original word as search term |
| DuckDuckGo failure | 200 | Falls back to Wikipedia URL |
| OpenAI failure | 500 | Exception after 10 retries |

### Client-Side Resilience

- **Clue URL validation**: HEAD request with 5s timeout, up to 5 retries
- **Excluded URL persistence**: Invalid URLs saved to localStorage, sent with future requests
- **Network errors**: Caught and displayed as user-friendly error messages
- **State recovery**: Game state re-fetched from server after each successful action

---

*Next: [05 — Clue Generation Pipeline](05-clue-generation-pipeline.md)*
