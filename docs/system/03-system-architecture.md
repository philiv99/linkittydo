# 03 — System Architecture

## High-Level Architecture

LinkittyDo follows a clean three-tier architecture with clear separation between presentation, business logic, and data access.

```
┌────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION TIER                           │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                    React + TypeScript + Vite                  │  │
│  │                                                              │  │
│  │  App.tsx ──► GameBoard ──► PhraseDisplay ──► WordSlot        │  │
│  │                              │                  │            │  │
│  │                              ├── GuessInput     ├── ClueBtn  │  │
│  │                              └── ScoreDisplay   └── CluePane │  │
│  │                                                              │  │
│  │  Hooks: useGame │ useUser │ useAudio                         │  │
│  │  Services: api.ts (HTTP client)                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                              │ HTTP/REST                           │
└──────────────────────────────┼─────────────────────────────────────┘
                               │
┌──────────────────────────────┼─────────────────────────────────────┐
│                        LOGIC TIER                                  │
│                               │                                    │
│  ┌────────────────────────────▼─────────────────────────────────┐  │
│  │                    ASP.NET Core 8 Web API                    │  │
│  │                                                              │  │
│  │  Controllers          Services            External APIs      │  │
│  │  ────────────         ────────────         ─────────────     │  │
│  │  GameController  ───► GameService                            │  │
│  │  ClueController  ───► ClueService    ───► Datamuse API       │  │
│  │  UserController  ───► UserService         (synonyms)         │  │
│  │  LlmController   ───► OpenAiLlmService ─► OpenAI API        │  │
│  │                       GamePhraseService    (phrase gen)       │  │
│  │                                       ───► DuckDuckGo        │  │
│  │                                            (URL search)      │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                              │                                     │
└──────────────────────────────┼─────────────────────────────────────┘
                               │
┌──────────────────────────────┼─────────────────────────────────────┐
│                        DATA TIER                                   │
│                               │                                    │
│  ┌────────────────────────────▼─────────────────────────────────┐  │
│  │                    Repository Pattern                        │  │
│  │                                                              │  │
│  │  IUserRepository ────────► JsonUserRepository                │  │
│  │  IGamePhraseRepository ──► JsonGamePhraseRepository          │  │
│  │                                                              │  │
│  │  Storage: Individual JSON files per entity                   │  │
│  │    Data/Users/USR-{ts}-{rand}.json                          │  │
│  │    Data/Phrases/PHR-{ts}-{rand}.json                        │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Frontend Framework | React 18 + TypeScript | Component-based UI |
| Build Tool | Vite | Fast HMR development |
| Linting | ESLint | Code quality |
| Backend Framework | ASP.NET Core 8 | REST API |
| Language | C# 12 | Service implementation |
| Synonym API | Datamuse | Lexical substitution |
| Search Engine | DuckDuckGo HTML | Clue URL discovery |
| LLM Provider | OpenAI (gpt-4o-mini) | Phrase generation |
| Data Storage | JSON files | Entity persistence |

---

## Backend Architecture

### Service Layer Design

Each service has a clear responsibility boundary:

```
┌─────────────────────────────────────────────────────────┐
│                    Service Layer                         │
│                                                         │
│  ┌─────────────────┐                                    │
│  │  GameService     │  Session management, guess         │
│  │                  │  validation, score tracking,       │
│  │                  │  event recording                   │
│  └────────┬─────────┘                                    │
│           │ depends on                                   │
│  ┌────────▼─────────┐                                    │
│  │ GamePhraseService│  Phrase selection, stop-word        │
│  │                  │  classification, tokenization,     │
│  │                  │  LLM phrase generation              │
│  └────────┬─────────┘                                    │
│           │ depends on                                   │
│  ┌────────▼─────────┐  ┌──────────────────┐              │
│  │  LlmService      │  │  ClueService     │              │
│  │  (OpenAI)        │  │                  │  Synonym      │
│  │                  │  │  Datamuse query, │  lookup,      │
│  │  Phrase          │  │  DuckDuckGo      │  search,      │
│  │  generation      │  │  search, URL     │  URL          │
│  │                  │  │  validation      │  selection     │
│  └──────────────────┘  └──────────────────┘              │
│                                                         │
│  ┌──────────────────┐                                    │
│  │  UserService     │  User CRUD, game history,          │
│  │                  │  points, difficulty                 │
│  └──────────────────┘                                    │
└─────────────────────────────────────────────────────────┘
```

### Dependency Injection Registration

```csharp
// Program.cs - Service registration order
builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();
builder.Services.AddSingleton<IGamePhraseRepository, JsonGamePhraseRepository>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IGamePhraseService, GamePhraseService>();
builder.Services.AddHttpClient<IClueService, ClueService>();
builder.Services.AddHttpClient<ILlmService, OpenAiLlmService>();
builder.Services.AddScoped<IUserService, UserService>();
```

### In-Memory Session Store

Active game sessions are held in a `Dictionary<Guid, GameSession>` within `GameService`. This is intentionally simple:

| Aspect | Current | Future |
|--------|---------|--------|
| Storage | In-memory dictionary | Redis/distributed cache |
| Lifetime | Until process restarts | Configurable TTL |
| Concurrency | Single-server safe | Distributed locking |
| Capacity | Bounded by memory | Horizontally scalable |

---

## Repository Pattern

### Interface Contracts

Both repositories follow the same pattern — async CRUD operations with thread-safe JSON file access:

```
IUserRepository                    IGamePhraseRepository
━━━━━━━━━━━━━━━━                   ━━━━━━━━━━━━━━━━━━━━━
GetByIdAsync(id)                   GetAllAsync()
GetByEmailAsync(email)             GetByIdAsync(id)
GetByNameAsync(name)               GetByTextAsync(text)
GetAllAsync()                      CreateAsync(phrase)
CreateAsync(user)                  CreateManyAsync(phrases)
UpdateAsync(user)                  DeleteAsync(id)
DeleteAsync(id)                    ExistsByTextAsync(text)
ExistsByEmailAsync(email)          GetCountAsync()
ExistsByNameAsync(name)
```

### JSON File Storage

Each entity is stored as an individual JSON file:

```
Data/
├── Users/
│   ├── USR-1736588400000-A1B2C3.json
│   ├── USR-1736589200000-D4E5F6.json
│   └── ...
└── Phrases/
    ├── PHR-1768156879293-99FDA7.json
    ├── PHR-1768156931675-2F2176.json
    └── ...
```

**Thread safety**: `SemaphoreSlim(1, 1)` ensures single-writer access for all file operations.

**Why JSON files?** They are human-readable, require zero infrastructure, and can be version-controlled. The repository interface allows swapping to SQL or NoSQL without changing any service code.

### Entity ID Formats

| Entity | Prefix | Format | Example |
|--------|--------|--------|---------|
| User | `USR-` | `USR-{unix_ms}-{6_hex}` | `USR-1736588400000-A1B2C3` |
| Phrase | `PHR-` | `PHR-{unix_ms}-{6_hex}` | `PHR-1768156879293-99FDA7` |
| Game | `GAME-` | `GAME-{unix_ms}-{6_hex}` | `GAME-1736588400000-D4E5F6` |

---

## External API Integration

### Datamuse — Synonym Discovery

```
Request Flow:
  ClueService
    │
    ├─ GET https://api.datamuse.com/words?rel_syn={word}&max=15
    │     → strict synonyms
    │
    └─ GET https://api.datamuse.com/words?ml={word}&max=15
          → meaning-like words (broader semantic field)

  Both requests execute in parallel (Task.WhenAll)
  Results are merged and deduplicated
```

**Rate limits**: Datamuse is free and does not require an API key. No documented rate limits, but the system makes at most 2 requests per clue.

### DuckDuckGo — URL Search

```
Request Flow:
  ClueService
    │
    └─ GET https://html.duckduckgo.com/html/?q={synonym}
          → HTML response with search results
          → Parse href attributes from result__a elements
          → Extract actual URLs from DDG redirect format
          → Filter: remove DDG/Google/Bing domains
          → Exclude previously used URLs
          → Random selection from remaining
          → Fallback: Wikipedia /wiki/{synonym}
```

### OpenAI — Phrase Generation

```
Request Flow:
  GamePhraseService → OpenAiLlmService
    │
    └─ POST https://api.openai.com/v1/chat/completions
         Model: gpt-4o-mini (configurable)
         System prompt: phrase generation rules
         User prompt: "Generate 10 unique phrases"
         → Parse line-by-line
         → Validate word count (2-7)
         → Deduplicate against existing phrase store
         → Persist new phrases as JSON files
```

---

## Frontend Architecture

### Component Hierarchy

```
App
 └─ GameBoard                     ← Main container, owns game lifecycle
     ├─ UserModal                 ← Login/register modal
     ├─ UserManageModal           ← Profile management
     ├─ ScoreDisplay              ← Points counter
     ├─ PhraseDisplay             ← Phrase layout engine
     │   └─ WordSlot (×N)        ← Per-word rendering
     │       ├─ GuessInput        ← Text input for hidden words
     │       ├─ ClueButton        ← Trigger clue fetch
     │       └─ CluePanel         ← Inline iframe display
     └─ LlmTestButton            ← Debug: test LLM connectivity
```

### Hook Architecture

| Hook | State Managed | API Methods |
|------|--------------|-------------|
| `useGame` | `gameState`, `loading`, `error` | `startGame`, `submitGuess`, `getClue`, `giveUp` |
| `useUser` | `user`, `isLoggedIn`, `loading` | `login`, `register`, `updateUser`, `addPoints` |
| `useAudio` | Audio playback state | `playCorrect`, `playIncorrect`, `playWin` |

### API Client (`api.ts`)

Centralized HTTP client with typed request/response handling:

```typescript
const api = {
  startGame(request?: StartGameRequest): Promise<GameState>,
  getGame(sessionId: string): Promise<GameState>,
  submitGuess(sessionId: string, request: GuessRequest): Promise<GuessResponse>,
  getClue(sessionId: string, wordIndex: number, excludeUrls?: string[]): Promise<ClueResponse>,
  giveUp(sessionId: string): Promise<GameState>,
  // ... user CRUD methods
};
```

---

## CORS & Deployment

### Development

```
Frontend: http://localhost:5173 (Vite dev server)
Backend:  http://localhost:5170 (Kestrel)
CORS:     Configured to allow localhost:5173
```

### Production

The API is deployed to Azure App Service. Frontend talks to the deployed API URL configured in `api.ts`.

---

## Scalability Considerations

| Component | Current (Single Server) | Scaled (Multi-Server) |
|-----------|------------------------|----------------------|
| Game sessions | In-memory dictionary | Redis with TTL |
| User storage | JSON files | PostgreSQL/CosmosDB |
| Phrase storage | JSON files | PostgreSQL/CosmosDB |
| Clue cache | None | Redis cache for synonym → URL mappings |
| LLM calls | Direct OpenAI | Queue-based with Azure OpenAI |
| Search | DuckDuckGo scraping | Own Lucene/Elasticsearch index |

The original architecture documents describe an ambitious vision including a **Heritrix web crawler** and **Lucene indexer** for building a proprietary URL database — a "LinkittyDo search engine" that returns results pre-optimized for clue quality.

---

*Next: [04 — Gameplay Loop & State Machine](04-gameplay-loop-and-state.md)*
