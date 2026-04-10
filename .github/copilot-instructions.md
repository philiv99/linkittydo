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
    public string UniqueId { get; set; }        // Format: USR-{timestamp}-{random}
    public string Name { get; set; }            // 2-50 chars, unique
    public string Email { get; set; }           // Valid email, unique
    public string PasswordHash { get; set; }    // BCrypt hash
    public int LifetimePoints { get; set; }     // Cumulative points earned
    public int PreferredDifficulty { get; set; } // 0-100, default 10
    public bool IsActive { get; set; }          // Soft-delete flag
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }    // Soft-delete timestamp
}
```

**Note**: GameRecords are a separate aggregate (since Sprint 13). Access game history via `IGameRecordRepository`, not through the User model.
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

The data access layer uses the Repository pattern with EF Core and a feature-flag switchover:

```
IUserRepository (interface)
    ├── JsonUserRepository (JSON file storage — legacy fallback)
    └── EfUserRepository (MySQL via EF Core — current default)

IGameRecordRepository (interface)
    ├── JsonGameRecordRepository (JSON file storage — legacy fallback)
    └── EfGameRecordRepository (MySQL via EF Core — current default)

IGamePhraseRepository (interface)
    ├── JsonGamePhraseRepository (JSON file storage — legacy fallback)
    └── EfGamePhraseRepository (MySQL via EF Core — current default)
```

### Current Implementation: MySQL via EF Core

- **ORM**: Entity Framework Core with Pomelo MySQL provider
- **DbContext**: `LinkittyDoDbContext` with Fluent API configurations (16 DbSets)
- **Migrations**: EF Core code-first with auto-migration at startup
- **DI Lifetime**: Repositories are Scoped (EF Core DbContext is not thread-safe)
- **Unit of Work**: `IUnitOfWork` wrapping `SaveChangesAsync()` + `BeginTransactionAsync()`
- **Soft-delete**: Users and GamePhrases use `IsActive`/`DeletedAt` columns

### Data Provider Feature Flag

The `DataProvider` setting in `appsettings.json` selects the storage backend:

```json
{
  "DataProvider": "MySql"
}
```

- `"MySql"` (default): Uses EF Core repositories with MySQL
- `"Json"`: Falls back to JSON file repositories (legacy)

### Session State

Game sessions use a separate Singleton `ISessionStore` (`InMemorySessionStore` with `ConcurrentDictionary`) to survive across Scoped DI lifetimes. Sessions are persisted to MySQL via `GameSessions` table with configurable TTL cleanup.

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

## Lessons Learned (Sprint Retrospective Registry)

These lessons are extracted from sprint retrospectives. Check them before making changes.

### Build & Verification
- **L1** (Sprint 33): Always use `npm run build` (not `npx tsc --noEmit`) as the definitive frontend build check. `tsc -b` catches unused imports and stricter errors.
- **L2** (Sprint 33): Run the full build early during frontend refactors (after first major file change), not just at the end.

### Refactoring
- **L3** (Sprint 13): When removing a property, function, or file, ALWAYS grep the entire codebase first. Property removal broke unexpected files that were not checked.
- **L4** (Sprint 11): When using EF Core discriminators with polymorphic models that have abstract computed properties, use shadow properties instead of mapping the abstract property directly.

### API Integration
- **L5** (Sprint 28): Before calling any backend endpoint from frontend, verify parameter order, types, and defaults. API signature mismatches cause runtime failures.
- **L6** (Sprint 12): When migrating Singleton services to Scoped lifetime, extract the stateful component into a Singleton first (e.g., `ISessionStore`).

### Testing
- **L7** (Sprints 28, 29, 33): New code MUST have corresponding tests specified in the sprint plan. Do not defer test writing.
- **L8** (Sprint 6): When tests use reflection to access private helpers, they are fragile to refactoring. Prefer testing through public interfaces.
- **L9** (Sprint 14): xUnit test projects targeting newer .NET need explicit `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

### Sprint Process
- **L10** (Sprint 9): Pre-sprint research (checking what is already done, validating assumptions) saves significant effort. Always verify backlog items are still relevant before planning.
- **L11** (Sprint 3): When a sprint plan changes scope, update BACKLOG.md sprint columns immediately to prevent downstream confusion.

---

## Backend Patterns

### Repository Pattern
- Data access abstracted via repository interfaces (`IUserRepository`, `IGameRecordRepository`, `IGamePhraseRepository`)
- Current implementation: EF Core with MySQL (Pomelo provider)
- Legacy fallback: JSON file-based repositories (switchable via `DataProvider` flag)
- Repository handles persistence, Service handles business logic
- `IUnitOfWork` for cross-repository transactional consistency

### Service Layer
- All business logic in Service classes with interface-based DI
- Controllers only handle HTTP concerns
- Key services: `GameService`, `ClueService`, `UserService`, `AuthService`, `RoleService`, `AuditService`, `SiteConfigService`, `AnalyticsService`, `SimulationService`, `AdminService`, `GamesManagerService`, `DataExplorerService`

### Authentication & Authorization
- JWT tokens with refresh token rotation
- BCrypt password hashing
- Role-based authorization: Player, Moderator, Admin
- Claims-based policies: `[Authorize(Policy = "RequireAdmin")]`
- Unified AuthContext on frontend (single token for player and admin)

### EF Core Patterns
- `LinkittyDoDbContext` with 16 DbSets and Fluent API configurations
- Auto-migration at startup
- Soft-delete via `IsActive`/`DeletedAt` on Users and GamePhrases
- Single-table inheritance for polymorphic `GameEvent` (Discriminator column)
- Shadow properties for EF-specific mappings

### Model Organization
- Domain models in `Models/` folder
- Request DTOs: `{Entity}Request.cs`
- Response DTOs: `{Entity}Response.cs`

### Key Controllers
- `GameController` — Game session management
- `UserController` — User CRUD, leaderboard
- `AuthController` — Login, register, refresh tokens
- `AdminController` — Dashboard, user management, player analytics
- `GamesManagerController` — Game browsing, phrase stats
- `SiteConfigController` — Site configuration CRUD
- `DataExplorerController` — Data summaries, simulation data
- `AuditLogController` — Audit log queries
- `MigrationController` — JSON-to-MySQL data migration

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

1. **`docs/agile/sprint-status.json`** (in repo): Tracks current sprint number, status, branch, approval state, test metrics, and **per-task progress** (`tasks` array with `{id, title, status}`). Read this at the start of every sprint-related conversation.

2. **Copilot Memory** (`/memories/repo/`): Save sprint summaries after each retrospective. Include: sprint number, goal, result, key lesson, process changes made, and top backlog candidates.

3. **Context limit handling**: If approaching the context limit during execution:
   - Save progress to `/memories/repo/sprint-N-progress.md` with: branch name, completed tasks, remaining tasks, blockers, files being modified
   - Update `docs/agile/sprint-status.json` with task-level status
   - Commit and push all work in progress with `wip:` prefix
   - Notify the user that a new session is needed

4. **Context efficiency rules** (added Sprint 35):
   - Prefer targeted file reads over full-file reads
   - Use `multi_replace_string_in_file` for batch edits
   - Run builds/tests first, then only investigate failures
   - Do not re-read files already in context
   - Estimate task context cost before starting (Small: 5-10%, Medium: 15-25%, Large: 25-40%)

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
