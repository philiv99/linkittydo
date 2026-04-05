# LinkittyDo Copilot Instructions

This document contains coding standards and patterns for the LinkittyDo project.

## IMPORTANT: Workspace Isolation

**IMPORTANT**: Never change any files or folders in any other workspace folder except LinkittyDo. When working in this multi-root workspace (LinkittyDo and spamfilter-multi), restrict all modifications to the LinkittyDo root only.

---

## Project Overview

LinkittyDo is a word-guessing game with:
- **Backend**: ASP.NET Core 8 Web API (C#)
- **Frontend**: React with TypeScript and Vite

---

## User Management

### User Model

```csharp
public class User
{
    public string UniqueId { get; set; }      // Format: USR-{timestamp}-{random}
    public string Name { get; set; }          // 2-50 chars, unique
    public string Email { get; set; }         // Valid email, unique
    public int LifetimePoints { get; set; }   // Cumulative points earned
    public int PreferredDifficulty { get; set; } // 0-100, default 10
    public List<GameRecord> Games { get; set; } // Game history
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### UniqueId Generation Rules

User UniqueIds follow a specific format to ensure uniqueness and traceability:

**Format**: `USR-{timestamp}-{random}`

- **Prefix**: `USR-` identifies this as a user ID
- **Timestamp**: Unix timestamp in milliseconds (13 digits)
- **Random**: 6-character alphanumeric string for collision prevention

**Example**: `USR-1736588400000-A1B2C3`

**Backend Implementation** (C#):
```csharp
public static string GenerateUniqueId()
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var random = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
    return $"USR-{timestamp}-{random}";
}
```

**Frontend Implementation** (TypeScript):
```typescript
const generateUniqueId = (): string => {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8).toUpperCase();
  return `USR-${timestamp}-${random}`;
};
```

---

## Data Access Layer

### Repository Pattern

The data access layer uses the Repository pattern to abstract storage implementation:

```
IUserRepository (interface)
    ├── JsonUserRepository (JSON file storage)
    └── SqlUserRepository (future: relational database)
```

### Current Implementation: JSON File Storage

- Users are stored as individual JSON files in `Data/Users/` directory
- File naming: `{uniqueId}.json`
- Thread-safe with SemaphoreSlim for concurrent access
- Data directory is configurable via `appsettings.json`:
  ```json
  {
    "DataDirectory": "path/to/data"
  }
  ```

### Swapping to Database

To switch to a relational database:
1. Implement `IUserRepository` with database logic
2. Update DI registration in `Program.cs`:
   ```csharp
   // Replace this:
   builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();
   // With this:
   builder.Services.AddScoped<IUserRepository, SqlUserRepository>();
   ```

---

## Game Tracking

### Overview
Games are tracked for registered users only. Guest users (name = "Guest" or no email) do not have their game data saved. Each game for a registered user tracks:
- Game metadata (ID, phrase, difficulty, timestamps)
- Ordered list of events (clues, guesses, game end)
- Final result (Solved or GaveUp)

### GameRecord Model

```csharp
public class GameRecord
{
    public string GameId { get; set; }        // Format: GAME-{timestamp}-{random}
    public DateTime PlayedAt { get; set; }     // When game started
    public DateTime? CompletedAt { get; set; } // When game ended
    public int Score { get; set; }             // Final score
    public int PhraseId { get; set; }          // Phrase identifier
    public string PhraseText { get; set; }     // Full phrase text
    public int Difficulty { get; set; }        // Difficulty when played
    public GameResult Result { get; set; }     // InProgress, Solved, GaveUp
    public List<GameEvent> Events { get; set; } // Ordered event history
}
```

### GameId Generation Rules

**Format**: `GAME-{timestamp}-{random}`

- **Prefix**: `GAME-` identifies this as a game ID
- **Timestamp**: Unix timestamp in milliseconds (13 digits)
- **Random**: 6-character alphanumeric string for collision prevention

**Example**: `GAME-1736588400000-A1B2C3`

### Game Events

Events are polymorphic and stored in an ordered list:

#### ClueEvent
Recorded when a player requests a clue for a word.
```csharp
public class ClueEvent : GameEvent
{
    public int WordIndex { get; set; }      // Index of word clue was for
    public string SearchTerm { get; set; }  // Synonym used for search
    public string Url { get; set; }         // URL shown as clue
}
```

#### GuessEvent
Recorded when a player submits a guess.
```csharp
public class GuessEvent : GameEvent
{
    public int WordIndex { get; set; }      // Index of word guessed
    public string GuessText { get; set; }   // The actual guess
    public bool IsCorrect { get; set; }     // Whether guess was correct
    public int PointsAwarded { get; set; }  // 100 if correct, 0 if not
}
```

#### GameEndEvent
Recorded when the game ends.
```csharp
public class GameEndEvent : GameEvent
{
    public string Reason { get; set; } // "solved" or "gaveup"
}
```

### Game Result Enum
```csharp
public enum GameResult
{
    InProgress, // Game is still active
    Solved,     // Player solved the phrase
    GaveUp      // Player clicked "I give up"
}
```

### API Endpoints

#### Start Game
- **POST** `/api/game/start`
- **Request Body** (optional):
  ```json
  {
    "userId": "USR-1736588400000-A1B2C3",
    "difficulty": 10
  }
  ```
- If `userId` is omitted or null, game runs in guest mode (no tracking)

#### Get Game Record
- **GET** `/api/game/{sessionId}/record`
- **Response** (200): Full GameRecord with events
- **Errors**: 404 if session not found or guest session

#### Get User's Game History
- **GET** `/api/user/{uniqueId}/games`
- **Response** (200): Array of GameRecord ordered by playedAt descending

---

## API Request/Response Standards

### HTTP Methods & Status Codes

| Operation | Method | Success Code | Error Codes |
|-----------|--------|--------------|-------------|
| Create    | POST   | 201 Created  | 400, 409    |
| Read      | GET    | 200 OK       | 404         |
| Update    | PUT    | 200 OK       | 400, 404    |
| Partial Update | PATCH | 200 OK  | 400, 404    |
| Delete    | DELETE | 204 No Content | 404       |

### Standard Response Format

#### Success Response
```json
{
  "data": { ... },
  "message": "Operation successful"
}
```

#### Error Response
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message"
  }
}
```

### User API Endpoints

#### Create User
- **POST** `/api/user`
- **Request Body**:
  ```json
  {
    "name": "string (required, 2-50 chars)",
    "email": "string (required, valid email format)"
  }
  ```
- **Response** (201):
  ```json
  {
    "uniqueId": "USR-1736588400000-A1B2C3",
    "name": "PlayerName",
    "email": "player@example.com",
    "lifetimePoints": 0,
    "preferredDifficulty": 10,
    "gamesPlayed": 0,
    "createdAt": "2026-01-11T12:00:00Z"
  }
  ```
- **Errors**:
  - 400: Invalid input (validation errors)
  - 409: Name or email already exists

#### Get User by ID
- **GET** `/api/user/{uniqueId}`
- **Response** (200): User object
- **Errors**: 404 if not found

#### Get User by Email
- **GET** `/api/user/by-email/{email}`
- **Response** (200): User object
- **Errors**: 404 if not found

#### Update User
- **PUT** `/api/user/{uniqueId}`
- **Request Body**: Same as create
- **Response** (200): Updated user object
- **Errors**: 400, 404, 409

#### Delete User
- **DELETE** `/api/user/{uniqueId}`
- **Response**: 204 No Content
- **Errors**: 404 if not found

#### Update Difficulty
- **PATCH** `/api/user/{uniqueId}/difficulty`
- **Request Body**:
  ```json
  {
    "difficulty": 50
  }
  ```
- **Response** (200):
  ```json
  {
    "uniqueId": "USR-1736588400000-A1B2C3",
    "preferredDifficulty": 50
  }
  ```
- **Errors**: 400 (invalid range), 404 (not found)

#### Add Points
- **POST** `/api/user/{uniqueId}/points`
- **Request Body**:
  ```json
  {
    "points": 100
  }
  ```
- **Response** (200):
  ```json
  {
    "uniqueId": "USR-1736588400000-A1B2C3",
    "lifetimePoints": 1500,
    "pointsAdded": 100
  }
  ```
- **Errors**: 400 (negative points), 404 (not found)

#### Check Name Availability
- **GET** `/api/user/check-name/{name}`
- **Response** (200):
  ```json
  {
    "available": true
  }
  ```

#### Check Email Availability
- **GET** `/api/user/check-email/{email}`
- **Response** (200):
  ```json
  {
    "available": true
  }
  ```

---

## Validation Rules

### User Name
- Required
- Length: 2-50 characters
- Allowed characters: letters, numbers, spaces, underscores, hyphens
- Must be unique (case-insensitive)

### User Email
- Required
- Must be valid email format
- Must be unique (case-insensitive)

### Preferred Difficulty
- Range: 0-100
- Default: 10

### Points
- Must be non-negative (>= 0)

---

## Error Codes

| Code | Description |
|------|-------------|
| `VALIDATION_ERROR` | Input validation failed |
| `NAME_TAKEN` | Username already exists |
| `EMAIL_TAKEN` | Email already registered |
| `USER_NOT_FOUND` | User with given ID not found |
| `INVALID_EMAIL` | Email format is invalid |
| `INVALID_DIFFICULTY` | Difficulty out of range (0-100) |
| `INVALID_POINTS` | Points must be non-negative |

---

## Backend Patterns

### Repository Pattern
- Data access abstracted via `IUserRepository` interface
- Implementations can be swapped (JSON files, SQL database, etc.)
- Repository handles persistence, Service handles business logic

### Service Pattern
- All business logic in Service classes
- Interface-based for dependency injection
- Controllers only handle HTTP concerns

### Model Organization
- Domain models in `Models/` folder
- Request DTOs: `{Entity}Request.cs`
- Response DTOs: `{Entity}Response.cs`

---

## Frontend Patterns

### State Management
- Use React hooks for local state
- Custom hooks for feature logic (e.g., `useUser`, `useGame`)
- localStorage for persistence

### API Communication
- All API calls in `services/api.ts`
- Type-safe request/response handling
- Error handling with try/catch

### Component Structure
- One component per file
- CSS modules or separate CSS files
- Props interfaces defined at top of file

---

## Sprint-Based Development

All development follows sprint-based planning with structured execution phases. Each sprint includes a learning loop where retrospective findings are applied to the process documents, improving the system sprint-over-sprint.

### Sprint Execution Documents

| Document | Purpose | Location |
|----------|---------|----------|
| **BACKLOG.md** | Prioritized work items | `docs/agile/BACKLOG.md` |
| **SPRINT_EXECUTION_WORKFLOW.md** | 5-phase execution checklist | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` |
| **SPRINT_STOPPING_CRITERIA.md** | When/why to stop working | `docs/agile/SPRINT_STOPPING_CRITERIA.md` |
| **SPRINT_RETROSPECTIVE_TEMPLATE.md** | Structured retrospective guide | `docs/agile/SPRINT_RETROSPECTIVE_TEMPLATE.md` |
| **sprint-status.json** | Sprint state persistence | `docs/agile/sprint-status.json` |
| **SPRINT_N_PLAN.md** | Per-sprint plan (created at planning) | `docs/agile/sprints/` |
| **SPRINT_N_RETRO.md** | Per-sprint retrospective | `docs/agile/sprints/` |

### Sprint Workflow (Phases 0-5)

| Phase | Name | User Required |
|-------|------|--------------|
| 0 | Prerequisites | No |
| 1 | Planning | **Yes** - approve plan |
| 2 | Kickoff | No |
| 3 | Execution | No (autonomous) |
| 4 | Review & Testing | **Yes** - review PR |
| 5 | Retrospective | **Yes** - provide feedback |

### How a Sprint Starts

A new sprint is triggered by the user saying "let's plan a sprint" or invoking `/plan-sprint`. There is no automatic trigger. Before planning, Phase 0 checks prerequisites (previous sprint merged, retro exists, builds pass).

### Execution Autonomy

Once the user approves a sprint plan (Phase 1), proceed through all tasks without stopping to ask for per-task approval. Make best engineering judgment for implementation decisions.

**Only stop for valid reasons** defined in `docs/agile/SPRINT_STOPPING_CRITERIA.md`:
- All tasks complete (normal completion)
- Blocked on external dependency
- User requests scope change
- Bug discovery (severity-based triage: only Critical bugs pause work)
- User requests early review
- Context limit approaching (save state at 85%, stop at 95%)
- Test failure escalation (new tests MUST pass; escalate if stuck)
- Fundamental design failure (requires rethinking approach)
- Time limit reached (sprint boundary)

### Test Failure Protocol

All new tests introduced during a sprint MUST pass before the sprint can be marked complete. If a test fails and the fix is not straightforward, attempt to fix (15-30 min), try an alternative approach (15-30 min), then request user approval to defer with a documented reason. Do NOT accept failing new tests without explicit user approval.

### The Learning Loop (Phase 5)

The retrospective is not just documentation - it is how the system improves:

1. **Review**: Summarize what was delivered vs planned
2. **Feedback**: User rates the sprint and provides input
3. **Identify**: Propose specific improvements with priorities (High/Medium/Low)
4. **Apply**: High-priority improvements are MANDATORY - apply them to the feature branch before the PR is merged. They update the relevant process docs (workflow, stopping criteria, agent files, copilot-instructions, etc.)
5. **Document**: Create `SPRINT_N_RETRO.md` recording findings and changes made
6. **Persist**: Save sprint context to memory and update `sprint-status.json`
7. **Verify**: The next sprint's Phase 0 verifies that previous retro improvements were actually applied

This means each sprint's lessons are baked into the process docs. The next sprint runs the improved process. The PR cannot merge until the retro is complete and High-priority improvements are applied.

### Memory and Context Persistence

Sprint context must survive across conversation sessions. Use these mechanisms:

1. **`docs/agile/sprint-status.json`** (in repo): Tracks current sprint number, status, branch, approval state, and test metrics. Read this at the start of every sprint-related conversation.

2. **Copilot Memory** (`/memories/repo/`): Save sprint summaries after each retrospective. Include: sprint number, goal, result, key lesson, process changes made, and top backlog candidates.

3. **Context limit handling**: If approaching the context limit during execution:
   - Save progress to `/memories/repo/` and `sprint-status.json`
   - Commit and push all work in progress
   - Notify the user that a new session is needed

When resuming a sprint in a new session:
- Read `docs/agile/sprint-status.json` for current state
- Check `/memories/repo/` for saved context
- Verify the feature branch and continue from where work stopped

### Agents and Skills

**Agents** (`.github/agents/`):
- `build-validator` - Validates backend and frontend builds
- `code-architect` - Architecture reviews and design decisions
- `code-simplifier` - Code simplification without behavior changes
- `verify-app` - Thorough application verification after changes

**Skills** (`.github/skills/`):
- `/plan-sprint` - Sprint planning and task breakdown
- `/phase-check` - Verify phase completion before transitioning
- `/full-test` - Run all tests and code analysis

### Common Commands

**Backend**:
```powershell
cd src/LinkittyDo.Api
dotnet restore
dotnet build
dotnet test
dotnet run
```

**Frontend**:
```powershell
cd src/linkittydo-web
npm ci
npm run dev      # Development server
npm run build    # Production build
npm test         # Run tests
npx tsc --noEmit # Type check
npx eslint src/  # Lint
```

### Changelog Policy

Update `CHANGELOG.md` in the same commit as code changes during sprint execution:
- Format: `- **type**: Description (Issue #N)`
- Types: `feat`, `fix`, `chore`, `docs`, `test`
- Add under `## [Unreleased]` section, grouped by date (newest first)

### Git Branching

```
main (release branch)
  └── feature/YYYYMMDD-sprint-N (sprint feature branches)
```

- Sprint work happens on feature branches
- PRs target `main` after sprint review
- Commit messages reference GitHub issues: `feat: add feature (#12)`
