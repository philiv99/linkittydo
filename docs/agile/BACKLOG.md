# LinkittyDo Backlog

Master backlog of all planned work for LinkittyDo. Items are prioritized and grouped by category. This is the single source of truth for what to build next.

**Last Updated**: 2026-04-10
**Source Analysis**: See [DESIGN_CONTENT_ANALYSIS.md](DESIGN_CONTENT_ANALYSIS.md) for the full gap assessment that generated this backlog.

---

## How to Use This Document

- **Sprint planning**: Select items from the top of each priority group
- **After each sprint**: Remove completed items, add new discoveries, re-prioritize
- **Adding items**: Add to the appropriate category with a priority level
- **Sprint mapping**: See [Sprint Roadmap](#sprint-roadmap) below for the recommended execution order

### Priority Levels

| Priority | Meaning |
|----------|---------|
| P1 - Critical | Must be done next; blocks other work or core functionality |
| P2 - High | Important for the product; should be in an upcoming sprint |
| P3 - Medium | Valuable but not urgent; schedule when capacity allows |
| P4 - Low | Nice to have; do when convenient |

---

## Backlog Items

### Foundation & Testing

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 1 | Backend test suite (xUnit) | P1 | 1 | ZERO tests exist. Must add unit tests for GameService, ClueService, UserService, GamePhraseService, and all controllers. Blocks all future sprints. |
| 2 | Frontend test suite (Vitest + Testing Library) | P1 | 1 | Install Vitest, add tests for hooks (useGame, useUser), API service, and key components (GameBoard, WordSlot). |
| 3 | API response standardization | P1 | 1 | Controllers return raw objects; must wrap in `{ data, message }` / `{ error: { code, message } }` per copilot-instructions spec. |

### Core Gameplay Enhancement

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 4 | Difficulty-aware clue selection | P1 | 2 | `PreferredDifficulty` is stored and sent but completely ignored. ClueService must select Xnym type and URL preference based on difficulty (see DESIGN_CONTENT_ANALYSIS.md §3A). |
| 5 | Enhanced scoring model | P1 | 2 | Replace flat 100 pts with `BasePoints / (n_clues × n_guesses)`. Add difficulty-scaled base points, first-guess bonus, speed bonus. See §3B. |
| 6 | Phrase difficulty computation | P2 | 2 | GamePhrase needs a `Difficulty` field computed from word frequency, hidden word ratio, phrase length. LLM generation prompt should request varied difficulty levels. |
| 7 | Scale phrase database | P2 | 2 | Only 26 phrases exist. Batch-generate 100+ phrases across difficulty bands. Add duplicate detection. |

### Frontend Architecture

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 8 | React Router + page structure | P2 | 3 | Add routing: `/` (home), `/play` (game), `/history`, `/leaderboard`, `/profile`. Move GameBoard to `/play` route. |
| 9 | Home page | P2 | 3 | Welcome content, quick-start button, user stats summary for logged-in users. Currently only a splash screen. |
| 10 | Game history UI | P2 | 3 | List past games with scores, phrases, results. Drill into game detail with event timeline replay. Data already exists on User.Games. |
| 11 | Responsive mobile layout | P2 | 4 | Desktop-only today. Design spec defines tablet/mobile breakpoints for PhraseDisplay, CluePanel, and word slot sizing. |

### Clue Quality & Linguistic Engine

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 12 | Xnym taxonomy expansion | P2 | 5 | Add Datamuse `rel_ant` (antonyms), `rel_trg` (triggers), `rel_hom` (homophones). Blend by difficulty level per formula in system doc 02. |
| 13 | Contextual synonym selection | P3 | 5 | Use Datamuse `lc=`/`rc=` to disambiguate polysemous words (e.g., "bank" in financial vs river context). |
| 14 | Clue caching layer | P3 | 5 | Pre-compute and cache synonym → URL mappings with 7-day TTL. Background validation of cached URLs. |

### Infrastructure & DevOps

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 15 | CI/CD pipeline (GitHub Actions) | P2 | 4 | Backend build + test, frontend build + lint + test, PR gate checks. No CI exists today. |
| 16 | Session TTL and cleanup | P2 | 4 | In-memory sessions persist forever until restart. Add configurable TTL (default 24h), background cleanup timer. |
| 17 | Health check improvements | P3 | 4 | Current `/health` returns minimal info. Add dependency checks (Datamuse, DuckDuckGo reachability, data directory). |

### User Experience & Engagement

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 18 | Leaderboard page | P2 | 6 | Ranked list by lifetime points. Backend endpoint for top-N users. Frontend page with table. |
| 19 | Timer and streak mechanics | P3 | 6 | Per-word timer (affects scoring), consecutive-correct streak multiplier, visual streak indicator. |
| 20 | Sound effects polish | P3 | 6 | Audio assets exist but integration is basic. Add difficulty-appropriate audio cues, volume control, mute toggle. |
| 21 | Accessibility improvements | P2 | 6 | ARIA labels for word slots, keyboard shortcuts (C=clue, G=give-up, N=new-game), `prefers-reduced-motion`, high-contrast mode, screen reader announcements. |

### Developer Experience

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 36 | One-click app launcher (bat file) | P1 | 2 | Create a single `.bat` file that starts both the backend API and frontend dev server and opens the app in a browser. Eliminates multi-step manual startup. |

### Game UI Overhaul

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 37 | Remove splash/click-to-start screens | P1 | 6 | Remove the audio click-to-start overlay, the splash screen with PLAY button, and the HomePage hero section. Game should start immediately on /play. |
| 38 | Professional game layout structure | P1 | 6 | Redesign GameBoard: fixed header, compact phrase bar, maximized tabbed clue panel as primary content area, footer with version/status. No more 50/50 split. |
| 39 | Compact phrase bar with action buttons | P1 | 6 | Top row showing phrase words with intuitive inline buttons for Get Clue, Clue History, and Guess. Phrase bar should be compact to maximize clue panel space. |
| 40 | Tabbed clue display as primary content | P1 | 6 | CluePanel should occupy most screen real estate (70-80% of viewport). Professional tab strip with word position badges. |
| 41 | App footer with version and status | P1 | 6 | Sticky footer showing app version, connection status, game timer, and keyboard shortcuts. Minimal height to preserve space. |
| 42 | Default route to /play | P2 | 6 | Root route `/` should redirect to `/play` instead of the HomePage splash. Remove or simplify HomePage. |

### Bug Fixes

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 112 | BUG: `Unknown column 'g.PhraseUniqueId' in 'field list'` — Migration not applied | P1 | 32 | The `AddPhraseUniqueIdToGameRecord` migration exists but was not applied to the running MySQL database. Any LINQ query touching `GameRecord.PhraseUniqueId` (e.g., `AnalyticsService.RecomputePhrasePlayStatsAsync`) throws `MySqlException`. Fix: apply pending migration, add startup migration check, verify all model-to-DB column mappings are consistent, add integration test. |
| 113 | BUG: Admin users forced to re-login when navigating to admin pages | P1 | 33 | Dual token system: player login stores JWT in `linkittydo_token`, but `AdminGuard` and `adminApi` only read from `linkittydo_admin_token`. Admin users who logged in as players must login a second time. Fix: unify to a single token, have `adminApi` use the shared token, update `AdminGuard`. |
| 114 | Introduce AuthContext for centralized auth state | P1 | 33 | Auth state is scattered across `useUser` hook, `api.ts` token functions, and `adminApi.ts` token functions. Create a React Context (`AuthContext`) providing token, user, roles, isAdmin to the entire component tree. Eliminates direct localStorage reads from multiple modules. |
| 115 | Remove redundant admin login page and token storage | P2 | 33 | `AdminLogin.tsx` duplicates the player login flow against the same backend endpoint. `adminApi.ts` maintains separate `ADMIN_TOKEN_KEY`/`ADMIN_REFRESH_TOKEN_KEY`. Remove these in favor of the unified auth layer. Redirect `/admin/login` to the main login flow. |

### Security & Production Readiness

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 22 | User authentication (JWT) | P1 | 7 | API is fully open. Add JWT token authentication, login/register flow with password, refresh tokens, protect user-specific endpoints. |
| 23 | Input sanitization audit | P2 | 7 | Review all API inputs for injection vectors. Validate guess text, user names, emails at API boundary. |
| 24 | Rate limiting | P2 | 7 | Prevent brute-force guessing and API abuse. Add rate limits on clue requests (per session) and auth endpoints. |

### Data Layer Evolution — MySQL Migration

_Source: `docs/sources/Database/` ER diagrams, `docs/system/06-data-models-and-storage.md` migration path, original class diagrams, implementation analysis of current repository interfaces._

#### Design Principles

The migration follows these software design principles:

1. **Repository per aggregate root** — Each top-level entity gets its own repository interface. GameRecord is currently embedded in User; it must be extracted to its own aggregate.
2. **Unit of Work** — Cross-repository operations (e.g., completing a game updates GameRecord AND User.LifetimePoints) require transactional consistency via `IUnitOfWork`.
3. **DI lifetime evolution** — Current Singleton repositories (for JSON + SemaphoreSlim) must become Scoped (EF Core DbContext is not thread-safe). Services that depend on repositories must also become Scoped.
4. **Feature-flag switchover** — appsettings toggle (`DataProvider: "Json" | "MySql"`) allows running both providers during migration. No big-bang cutover.
5. **Soft-delete by default** — Use `IsActive`/`DeletedAt` columns on Users and GamePhrases instead of hard deletes to preserve referential integrity in analytics.
6. **Audit trail** — Track create/update/delete with `CreatedAt`, `UpdatedAt`, `DeletedAt` columns on all core tables.

#### Phase A — EF Core Infrastructure & Repository Refactoring (Sprint 8a)

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 25 | EF Core + MySQL infrastructure | P2 | 8a | Add `Pomelo.EntityFrameworkCore.MySql`. Create `LinkittyDoDbContext` with entity configurations (Fluent API, not attributes). Connection string in appsettings per environment. Docker Compose for local MySQL 8.0+. EF Core CLI tooling (`dotnet-ef`). Initial empty migration to validate setup. |
| 26 | Repository interfaces refactoring | P2 | 8a | Extract `IGameRecordRepository` (currently GameRecords are embedded in User.Games). Add `IUnitOfWork` interface wrapping `SaveChangesAsync()` + `BeginTransactionAsync()`. Update `IUserRepository` to remove game-record concerns — User entity becomes a pure user profile aggregate. |
| 43 | Users table + EF Core repository | P2 | 8a | `Users` table: `UniqueId VARCHAR(30) PK`, `Name VARCHAR(50) NOT NULL`, `Email VARCHAR(255) NOT NULL`, `LifetimePoints INT DEFAULT 0`, `PreferredDifficulty INT DEFAULT 10`, `IsActive BIT DEFAULT 1`, `CreatedAt DATETIME(3) NOT NULL`, `UpdatedAt DATETIME(3) NULL`, `DeletedAt DATETIME(3) NULL`. Unique indexes on `Name` (case-insensitive collation) and `Email`. Implement `EfUserRepository : IUserRepository`. |
| 44 | GamePhrases table + EF Core repository | P2 | 8a | `GamePhrases` table: `UniqueId VARCHAR(30) PK`, `Text VARCHAR(500) NOT NULL UNIQUE`, `WordCount INT NOT NULL`, `Difficulty INT DEFAULT 0`, `GeneratedByLlm BIT DEFAULT 0`, `IsActive BIT DEFAULT 1`, `CreatedAt DATETIME(3) NOT NULL`. Implement `EfGamePhraseRepository : IGamePhraseRepository`. |
| 63 | DI lifetime migration (Singleton → Scoped) | P2 | 8a | Refactor service registrations: repositories become Scoped, services that depend on them become Scoped. `GameService` session dictionary must move to a separate Singleton `ISessionStore` (in-memory `ConcurrentDictionary`) so it survives across Scoped lifetimes. Validate no Singleton depends on a Scoped service (captive dependency). |
| 64 | Data provider feature flag | P2 | 8a | appsettings toggle `"DataProvider": "Json"` or `"MySql"`. Conditional DI registration selects Json or EF repositories. Both providers coexist in the codebase. Allows incremental rollout and easy rollback. |

#### Phase B — Game Data Normalization (Sprint 8b)

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 45 | GameRecords table + IGameRecordRepository | P2 | 8b | Extract from embedded `User.Games`. `GameRecords` table: `GameId VARCHAR(30) PK`, `UserId VARCHAR(30) NOT NULL` (FK → Users ON DELETE RESTRICT), `PhraseUniqueId VARCHAR(30) NULL` (FK → GamePhrases ON DELETE SET NULL), `PhraseText VARCHAR(500) NOT NULL`, `PlayedAt DATETIME(3) NOT NULL`, `CompletedAt DATETIME(3) NULL`, `Score INT DEFAULT 0`, `Difficulty INT DEFAULT 0`, `Result VARCHAR(20) NOT NULL DEFAULT 'InProgress'`. Index on `(UserId, PlayedAt DESC)` for history queries. New `IGameRecordRepository` with: `GetByGameIdAsync`, `GetByUserIdAsync(paged)`, `CreateAsync`, `UpdateAsync`. |
| 46 | GameEvents table (single-table inheritance) | P2 | 8b | `GameEvents` table: `Id BIGINT AUTO_INCREMENT PK`, `GameId VARCHAR(30) NOT NULL` (FK → GameRecords ON DELETE CASCADE), `SequenceNumber INT NOT NULL` (ordering within a game — Timestamp alone is not unique), `EventType VARCHAR(10) NOT NULL` (discriminator: 'clue' / 'guess' / 'gameend'), `WordIndex INT NULL`, `SearchTerm VARCHAR(200) NULL`, `Url VARCHAR(2048) NULL`, `GuessText VARCHAR(100) NULL`, `IsCorrect BIT NULL`, `PointsAwarded INT NULL`, `Reason VARCHAR(20) NULL`, `Timestamp DATETIME(3) NOT NULL`. Composite index `(GameId, SequenceNumber)`. Single-table inheritance is the right pattern here — the event types share enough columns and are always queried together per game. |
| 65 | User model refactoring (decouple Games) | P2 | 8b | Remove `List<GameRecord> Games` from User domain model. GameRecords are now a separate aggregate accessed via `IGameRecordRepository`. Update `UserService`, `GameService`, and all API DTOs that previously relied on `User.Games`. The `/api/user/{id}/games` endpoint now queries `IGameRecordRepository.GetByUserIdAsync()` instead of reading from User object. |
| 66 | JSON → MySQL data migration tool | P2 | 8b | CLI command (`dotnet run -- --migrate-data`) or EF Core data seed. Read all `Data/Users/*.json` (including embedded GameRecords and GameEvents) and `Data/Phrases/*.json`. Insert into normalized tables with FK relationships preserved. Idempotent (skip existing by PK). Verify row counts. Log any data integrity issues. Run as part of deployment, not at startup. |

#### Phase C — Session Persistence & Operational Tables (Sprint 8c)

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 47 | Session persistence (DB-backed) | P3 | 8c | `GameSessions` table: `SessionId CHAR(36) PK`, `UserId VARCHAR(30) NULL` (FK → Users), `PhraseUniqueId VARCHAR(30) NOT NULL`, `Score INT DEFAULT 0`, `Difficulty INT DEFAULT 0`, `StateJson JSON NOT NULL` (RevealedWords, UsedClueTerms, UsedClueUrls, ClueCountPerWord, GuessCountPerWord — ephemeral state that doesn't need its own columns), `StartedAt DATETIME(3) NOT NULL`, `LastActivityAt DATETIME(3) NOT NULL`. `SessionCleanupService` changes from clearing in-memory dictionary to DELETE WHERE `LastActivityAt < cutoff`. Games survive API restart. |
| 67 | MySQL health check endpoint | P3 | 8c | Extend `/health` to include MySQL connectivity check. Use ASP.NET Core `IHealthCheck` with `MySqlHealthCheck` that executes `SELECT 1`. Return degraded status if MySQL is unreachable but JSON fallback is active. |
| 68 | Database seeding (admin user, test data) | P3 | 8c | EF Core `HasData()` seed or startup service. Create initial admin user. In Development environment, seed sample phrases and a test user with game history. Idempotent (check before insert). |

#### Phase D — Authorization & Role Tables (Sprint 8d)

_Depends on Sprint 7 (JWT authentication). Roles extend the auth system, not replace it._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 48 | Roles & authorization tables | P2 | 8d | `Roles` table: `Id INT AUTO_INCREMENT PK`, `Name VARCHAR(50) NOT NULL UNIQUE` (seed: Player, Moderator, Admin). `UserRoles` junction: `UserId VARCHAR(30)` (FK → Users), `RoleId INT` (FK → Roles), `AssignedAt DATETIME(3)`, composite PK `(UserId, RoleId)`. Use ASP.NET Core claims-based authorization with custom `IClaimsTransformation` that reads roles from DB and adds as claims. Policy-based `[Authorize(Policy = "RequireAdmin")]` on admin endpoints. |
| 69 | Audit log table | P3 | 8d | `AuditLog` table: `Id BIGINT AUTO_INCREMENT PK`, `UserId VARCHAR(30) NULL`, `Action VARCHAR(50) NOT NULL` (UserCreated, GameStarted, PhraseApproved, RoleChanged, etc.), `EntityType VARCHAR(50) NULL`, `EntityId VARCHAR(30) NULL`, `Details JSON NULL`, `IpAddress VARCHAR(45) NULL`, `Timestamp DATETIME(3) NOT NULL`. Index on `(EntityType, EntityId)` and `(UserId, Timestamp)`. Write-only append table for compliance and debugging. |

#### Phase E — Content Management & Categorization (Sprint 9a)

_Source: ER diagram "Games, Phrases, Parses & Profiles", wireframe "3 - Games Mgr"._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 49 | Site configuration table | P3 | 9a | `SiteConfig` table: `Key VARCHAR(100) PK`, `Value TEXT NOT NULL`, `ValueType VARCHAR(20) NOT NULL` (string / int / bool / json), `Description VARCHAR(500) NULL`, `UpdatedAt DATETIME(3) NOT NULL`, `UpdatedBy VARCHAR(30) NULL` (FK → Users). Parameters: MaxSessionTtlHours, DefaultDifficulty, ClueRetryLimit, LlmBatchSize, MaintenanceMode. Accessed via `ISiteConfigService` with in-memory cache (refresh on update). |
| 50 | Phrase categories & tagging | P3 | 9a | `PhraseCategories` table: `Id INT AUTO_INCREMENT PK`, `Name VARCHAR(100) NOT NULL UNIQUE`, `Description VARCHAR(500) NULL`, `IsActive BIT DEFAULT 1`. `PhraseCategoryAssignments` junction: `PhraseUniqueId VARCHAR(30)` (FK → GamePhrases), `CategoryId INT` (FK → PhraseCategories), composite PK. Seed categories: Idioms, Proverbs, Quotes, Pop Culture, Science, Literature. Add `CategoryId` filter to phrase selection in `GamePhraseService`. |
| 51 | Phrase review workflow | P3 | 9a | `PhraseReviews` table: `Id INT AUTO_INCREMENT PK`, `PhraseUniqueId VARCHAR(30) NOT NULL` (FK → GamePhrases), `SubmittedBy VARCHAR(30) NOT NULL` (FK → Users), `ReviewedBy VARCHAR(30) NULL` (FK → Users), `Status VARCHAR(20) NOT NULL DEFAULT 'Pending'` (Pending / Approved / Rejected), `ReviewNotes TEXT NULL`, `SubmittedAt DATETIME(3) NOT NULL`, `ReviewedAt DATETIME(3) NULL`. Only Approved phrases enter the active game pool (add `Status` column to GamePhrases or filter via join). |

#### Phase F — Analytics & Computed Tables (Sprint 9b)

_Source: `docs/system/07-analytics-and-learning.md`. These are derived/computed tables, not raw storage — raw data lives in GameRecords + GameEvents._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 52 | Clue effectiveness materialized table | P3 | 9b | `ClueEffectiveness` table: `Id BIGINT AUTO_INCREMENT PK`, `TargetWord VARCHAR(100) NOT NULL`, `SearchTerm VARCHAR(200) NOT NULL`, `UrlDomain VARCHAR(255) NOT NULL`, `TimesShown INT DEFAULT 0`, `TimesLedToCorrectGuess INT DEFAULT 0`, `AvgGuessesAfterClue DECIMAL(5,2) NULL`, `LastComputedAt DATETIME(3) NOT NULL`. Unique index `(TargetWord, SearchTerm, UrlDomain)`. Recomputed by scheduled background job (not realtime). Powers synonym affinity matrix from system doc 07. |
| 53 | Player statistics computed table | P3 | 9b | `PlayerStats` table: `UserId VARCHAR(30) PK` (FK → Users), `GamesPlayed INT DEFAULT 0`, `GamesSolved INT DEFAULT 0`, `GamesGaveUp INT DEFAULT 0`, `AvgScore DECIMAL(8,2) DEFAULT 0`, `AvgCluesPerGame DECIMAL(5,2) DEFAULT 0`, `AvgGuessesPerGame DECIMAL(5,2) DEFAULT 0`, `BestScore INT DEFAULT 0`, `CurrentStreak INT DEFAULT 0`, `BestStreak INT DEFAULT 0`, `LastPlayedAt DATETIME(3) NULL`, `ComputedAt DATETIME(3) NOT NULL`. Refreshed incrementally on game completion (update counters) + full recompute nightly. Powers leaderboard and profile pages. |
| 54 | Phrase play statistics table | P3 | 9b | `PhrasePlayStats` table: `PhraseUniqueId VARCHAR(30) PK` (FK → GamePhrases), `TimesPlayed INT DEFAULT 0`, `TimesSolved INT DEFAULT 0`, `TimesGaveUp INT DEFAULT 0`, `SolveRate DECIMAL(5,4) DEFAULT 0`, `AvgCluesToSolve DECIMAL(5,2) NULL`, `AvgTimeToSolveSeconds DECIMAL(10,2) NULL`, `GiveUpRate DECIMAL(5,4) DEFAULT 0`, `CalibratedDifficulty INT NULL`, `LastComputedAt DATETIME(3) NOT NULL`. Auto-recomputed from GameRecords. Feeds back into phrase selection: `GamePhraseService` uses `CalibratedDifficulty` when matching player's `PreferredDifficulty`. |

### Admin & Management (Database-Backed)

_Source: Wireframes "6 - Admin.docx", "3 - Games Mgr_.docx", class diagram "Users, Players, Admin, Society"._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 70 | Admin nav link for admin users | P1 | 27 (DONE) | When logged-in user has Admin role (roleId 3), show Admin link in NavHeader. Roles returned in auth response and JWT. |
| 56 | Admin dashboard API | P3 | 10 | Endpoints: `GET /api/admin/dashboard` (total users, active sessions, phrase count, games today, top players). `GET /api/admin/users?page=&search=` (paginated, filterable). `PATCH /api/admin/users/{id}/role` (assign/remove roles). `GET /api/admin/audit-log?entity=&user=&from=&to=` (paginated audit trail). All require `[Authorize(Policy = "RequireAdmin")]`. |
| 57 | Admin user management UI | P3 | 10 | Frontend admin page: user list with search/filter, view/edit user details, change roles, soft-deactivate accounts (sets `IsActive = false`). Protected route requiring admin role. Source: wireframe "6 - Admin". |
| 58 | Games Manager API | P3 | 10 | Endpoints: `GET /api/admin/phrases?page=&category=&difficulty=&status=` (paginated, multi-filter). `POST /api/admin/phrases` (create with category assignment). `PUT /api/admin/phrases/{id}` (edit text, difficulty, categories). `POST /api/admin/phrases/{id}/evaluate` (starts a test-play session). `GET /api/admin/phrases/{id}/stats` (play statistics from #54). `POST /api/admin/phrases/{id}/review` (approve/reject from review queue). |
| 59 | Games Manager UI | P3 | 10 | Frontend phrase management: browse/search/filter phrases by category and difficulty, create new phrases manually or trigger LLM batch generation, edit existing, assign categories, view per-phrase play statistics (#54), review queue for community submissions (#51). Source: wireframe "3 - Games Mgr". |
| 60 | Site configuration admin UI | P4 | 10 | Frontend page to manage `SiteConfig` key-value pairs. Form with appropriate input types per `ValueType` (toggle for bool, slider for int ranges, textarea for json). Validation per parameter. Changes take effect after cache refresh. |

### Analytics & Intelligence

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 27 | Clue effectiveness tracking service | P3 | 9b | Background service that periodically recomputes `ClueEffectiveness` (#52) from `GameEvents`. Computes P(correct | synonym, URL domain) per word. Builds synonym affinity matrix per system doc 07. Optional: expose via `GET /api/analytics/clue-effectiveness?word=`. |
| 28 | Player analytics dashboard | P3 | 10 | Frontend dashboard showing aggregated player behavior from `PlayerStats` (#53): play frequency, difficulty progression over time, solve rates. Admin-only initially; later expose per-user stats on profile page. |
| 29 | Phrase difficulty auto-calibration | P3 | 9b | Background job that recomputes `PhrasePlayStats` (#54) and writes `CalibratedDifficulty` back to `GamePhrases.Difficulty`. Triggered after every N games or nightly. Closes the feedback loop: phrases that players find hard/easy auto-adjust. |

### Simulated Computer Player

_Purpose: Generate realistic gameplay data at scale to seed analytics, stress-test the system, calibrate phrase difficulty, and validate clue effectiveness — all without requiring real players. Simulation data is always distinguishable from real player data._

#### Design Principles

1. **Server-side only** — The simulator runs as a backend service, not through the public API. It has direct access to phrase answers (the API never exposes hidden words until guessed/given-up, making API-level simulation impractical).
2. **Clearly tagged data** — All simulated users use a `SIM-` prefix on their UniqueId (e.g., `SIM-1736588400000-A1B2C3`). All simulated GameRecords carry an `IsSimulated BIT` flag. Analytics queries can include or exclude simulated data with a simple WHERE clause.
3. **Configurable behavior profiles** — Simulated players have tunable parameters (skill level, clue usage patterns, give-up threshold) to generate diverse, realistic gameplay distributions.
4. **Non-destructive** — Simulation never modifies real user data. Simulated users and games can be purged independently without affecting real data.

#### Phase A — Simulation Infrastructure (Sprint 9c)

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 70 | IsSimulated flag on data models | P2 | 9c | Add `IsSimulated BIT NOT NULL DEFAULT 0` to `Users`, `GameRecords`, and `GameSessions` tables. Add matching property to C# domain models. Update `IUserRepository` and `IGameRecordRepository` with `bool includeSimulated = false` parameter on list/query methods so simulated data is excluded by default. All existing data gets `IsSimulated = 0`. EF Core migration. Depends on #43 (Users table), #45 (GameRecords table). |
| 71 | Simulated user ID format (SIM- prefix) | P2 | 9c | Simulated users use `SIM-{timestamp}-{random}` instead of `USR-`. Add `GenerateSimulatedUserId()` alongside existing `GenerateUniqueId()`. Frontend display logic shows a "Bot" badge or icon when rendering SIM- prefixed users on leaderboards or game history. Depends on #43 (Users table). |
| 72 | Simulation behavior profiles | P2 | 9c | Define `SimulationProfile` model with tunable parameters: `SkillLevel` (0.0–1.0, probability of guessing correctly given a clue), `ClueRequestRate` (avg clues before guessing, 1–5), `GiveUpThreshold` (max failed guesses before giving up, 1–10), `GuessAccuracy` (probability of correct guess per attempt), `PlaySpeed` (simulated delay between events for realistic timestamps). Seed default profiles: Novice (skill 0.3), Average (skill 0.6), Expert (skill 0.9), Impatient (low give-up threshold), Methodical (high clue usage). Stored as JSON config or in `SimulationProfiles` table. |

#### Phase B — Simulation Engine (Sprint 9c)

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 73 | ISimulationService + game simulation engine | P2 | 9c | Core service that plays a complete game programmatically. Flow: (1) Select phrase via `IGamePhraseService`, (2) For each hidden word: decide clue count from profile, generate clue events via `IClueService` (real synonym + URL lookup), decide guess outcome from profile's `SkillLevel`, record GuessEvent (correct answer from Phrase.Words — server-side access only), (3) Decide solve vs give-up based on profile. Produces a complete `GameRecord` with realistic `GameEvent` sequence. All records tagged `IsSimulated = true`. Depends on #70, #72, existing `IClueService`, `IGamePhraseService`. |
| 74 | Batch simulation runner | P2 | 9c | `ISimulationRunner` service that executes N games across M profiles. Configurable via appsettings or CLI arguments: `SimulationBatchSize` (games per run), `ProfileDistribution` (percentage per profile type), `DifficultyRange` (min-max difficulty to simulate). Can run as: (a) CLI command (`dotnet run -- --simulate --count=100`), (b) Admin API endpoint `POST /api/admin/simulation/run` with body `{ count, profiles, difficultyRange }`, (c) Background scheduled job (e.g., nightly). Logs progress and summary stats. Depends on #73. |
| 75 | Simulation data purge tool | P3 | 9c | Admin endpoint `DELETE /api/admin/simulation/data` to purge all simulated data: delete GameEvents → GameRecords → Users where `IsSimulated = true`. Also `DELETE /api/admin/simulation/data?olderThan=30d` for time-bounded cleanup. Cascading deletes handle FK relationships. Recompute `PlayerStats` and `PhrasePlayStats` after purge. Requires Admin role (#48). |

#### Phase C — Simulation Analytics Integration (Sprint 9c)

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 76 | Analytics dual-mode (real vs simulated vs combined) | P3 | 9c | Update all analytics computed tables (#52, #53, #54) and their recompute jobs to support three modes: `real-only` (exclude IsSimulated), `simulated-only`, `combined`. Admin dashboard (#56) exposes a toggle control. Default for player-facing views (leaderboard, profile): real-only. Default for admin analytics: combined. Stored as `SiteConfig` parameter. Depends on #70, #52, #53, #54. |
| 77 | Simulation summary dashboard | P3 | 10 | Admin UI section showing: total simulated games, distribution by profile type, simulated solve/give-up rates, comparison charts (simulated vs real player distributions). Helps validate that simulation profiles produce realistic data. Also shows per-phrase simulated statistics to identify phrases that are too easy/hard before real players encounter them. Depends on #74, #76, #56 (admin dashboard). |

### Admin Data Explorer (Database-Backed)

_Extends the Admin & Management section (#56-#60) with detailed data exploration views for players, games, and gameplay data. Source: Wireframes "6 - Admin.docx"._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 78 | Player detail view API & UI | P3 | 10 | `GET /api/admin/users/{id}/detail` returns: user profile, role assignments, `PlayerStats` (#53), recent game history (last 20 games with scores/results), account status, audit log entries for this user. Frontend: dedicated player detail page accessible from admin user list (#57). Shows profile card, stats summary, game history table with drill-down, role management, account actions (deactivate, reset password). |
| 79 | Game detail view API & UI | P3 | 10 | `GET /api/admin/games/{gameId}` returns: full `GameRecord` with all `GameEvents` in sequence, related user info, phrase info with play stats. Frontend: game detail page showing event timeline (visual sequence of clue → guess → clue → guess → ... → gameend), scoring breakdown per word, time between events, clue effectiveness for this game. Accessible from player detail (#78) or game browser (#80). Depends on #45, #46 (GameRecords + GameEvents tables). |
| 80 | Game browser API & UI | P3 | 10 | `GET /api/admin/games?page=&user=&phrase=&result=&from=&to=&simulated=` — paginated game list with multi-filter. Filter by user, phrase, result (Solved/GaveUp), date range, and simulated flag. Sortable by date, score, duration. Frontend: game browser page with filter sidebar, sortable table, row click navigates to game detail (#79). Depends on #45 (GameRecords table), #70 (IsSimulated flag). |
| 81 | Data summary & system overview | P3 | 10 | Extends admin dashboard (#56) with: total games (real vs simulated), games per day chart, phrase usage heatmap (most/least played phrases), average scores by difficulty band, active session count, database size. Powers the admin landing page. Depends on #53, #54 (computed stats tables), #70 (IsSimulated). |

### Database Initialization & Data Migration

| # | Item | Priority | Sprint | Status | Notes |
|---|------|----------|--------|--------|-------|
| 82 | Apply InitialCreate EF Core migration to MySQL | P1 | 25 | Done | All 17 tables created. Migration applied successfully. |
| 83 | Run JSON-to-MySQL data migration | P1 | 25 | Done | 3 users imported, 10 unique phrases imported. Duplicate phrase texts correctly rejected by unique constraint. |
| 84 | Update admin user seed with correct credentials | P1 | 25 | Done | Admin user seeded with name `admin`, password `tatyung86`, Admin role assigned. |
| 85 | Switch DataProvider to MySql | P1 | 25 | Done | Already set to `MySql` in appsettings. Verified all endpoints work. |

### Admin Frontend

| # | Item | Priority | Sprint | Status | Notes |
|---|------|----------|--------|--------|-------|
| 86 | Admin login page | P1 | 26 | Done | `/admin/login` with email/password, JWT storage, redirect to dashboard. |
| 87 | Admin route guards and layout | P1 | 26 | Done | AdminGuard checks JWT, AdminLayout with sidebar nav and Outlet. |
| 88 | Admin dashboard page | P1 | 26 | Done | Dashboard with stat cards (users, phrases, games, solve rate, avg score). |
| 89 | Admin user management page | P2 | 26 | Done | Paginated user list, activate/deactivate toggle, player analytics drill-down. |
| 90 | Admin games manager page | P2 | 26 | Done | Filterable game list, game detail with event timeline. |
| 91 | Admin site config page | P2 | 26 | Done | Type-aware inline editing (bool/int/json/string). |
| 92 | Admin data explorer page | P3 | 26 | Done | Data summary, simulation summary, player lookup. |

### Admin Completeness (Identified Sprint 28 Gap Analysis)

_Source: Gap analysis of existing admin pages, backend endpoints, and design wireframes. These items complete the admin panel to production-ready status._

| # | Item | Priority | Sprint | Status | Notes |
|---|------|----------|--------|--------|-------|
| 93 | Fix frontend TypeScript build errors | P1 | 28 | Done | Pre-existing TS errors in GameBoard, UserModal, useUser, tests, and vite.config. `tsc -b` fails but Vite dev server works. Must fix for production builds. |
| 94 | Admin Phrases Management page | P1 | 28 | Done | Backend has `GET /api/admin/games/phrase-stats/{id}` but NO frontend page. Need: phrase list with search/filter by category and difficulty, phrase stats display, phrase CRUD (add/edit/deactivate). Requires new backend endpoints for phrase listing and CRUD. |
| 95 | Admin user search and filter | P2 | 28 | Done | AdminUsers page has pagination but no search by name/email and no simulated user filter toggle. Backend API already supports `isSimulated` parameter. |
| 96 | Admin user role management | P2 | 28 | Done | AdminUsers page can toggle active status but cannot assign/change user roles (Admin, Moderator, Player). Backend `IRoleService` has `AssignRoleAsync`/`RemoveRoleAsync` but no admin API endpoint for role changes. |
| 97 | Confirmation dialogs for destructive actions | P2 | 29 | Done | User status toggle, config changes, and future delete actions execute immediately with no confirmation. Add modal confirmation for: user deactivation, config value changes, phrase deactivation. |
| 98 | Admin audit log viewer | P2 | 29 | Done | Backend `IAuditService` logs actions to `AuditLog` table but there is no API endpoint to query logs and no frontend viewer page. Need: `GET /api/admin/audit-log` endpoint with pagination/filters, admin page to browse/filter audit log entries. |
| 99 | Admin CSS cleanup (extract inline styles) | P3 | 29 | Done | 5 admin pages (Dashboard, Users, Games, Config, DataExplorer) use 300+ lines of inline styles. Extract to dedicated CSS files for maintainability. |
| 100 | Dashboard refresh and date filters | P3 | 29 | Done | AdminDashboard has no manual refresh button or auto-refresh. Add refresh button and optional date range filter. |
| 101 | Admin data export (CSV) | P3 | 30+ | | No export capability for user lists, game stats, or config. Add CSV export buttons to AdminUsers and AdminGames pages. |
| 102 | Admin phrase CRUD backend endpoints | P1 | 28 | Done | Need new endpoints: `GET /api/admin/phrases` (paginated list), `POST /api/admin/phrases` (create), `PUT /api/admin/phrases/{id}` (update), `PATCH /api/admin/phrases/{id}/status` (activate/deactivate). Required for phrase management UI (#94). |

### Admin Data Integrity & Defect Fixes

_Source: Comprehensive gap analysis of admin functionality (2026-04-10). Critical defects where game data is not correctly persisted, admin analytics show stale/wrong data, and admin navigation disappears._

| # | Item | Priority | Sprint | Status | Notes |
|---|------|----------|--------|--------|-------|
| 103 | Fix GameEvent persistence — events never saved to DB | P1 | 30 | Done | **CRITICAL DEFECT**: `LinkittyDoDbContext.ConfigureGameRecord()` calls `entity.Ignore(e => e.Events)` which tells EF Core to skip the Events navigation property. `EfGameRecordRepository.CreateAsync()` saves the GameRecord but events are silently discarded. The `GameEvents` table is always empty. Fix: after saving GameRecord, explicitly add each event via `_context.GameEvents.AddRange()` and `SaveChangesAsync()`. Must preserve SequenceNumber ordering and Discriminator values (clue/guess/gameend). |
| 104 | Recompute PlayerStats after game completion | P1 | 30 | Done | **DEFECT**: `AnalyticsService.RecomputePlayerStatsAsync()` has correct logic but is NEVER CALLED after a game completes. `PersistGameRecordAsync()` only saves the GameRecord and updates LifetimePoints. Admin player analytics show GamesPlayed=0, Streaks=0, BestScore=0. Fix: call `RecomputePlayerStatsAsync(userId)` in `PersistGameRecordAsync()` after successfully saving the game record. |
| 105 | Recompute PhrasePlayStats after game completion | P1 | 30 | Done | **DEFECT**: `AnalyticsService.RecomputePhrasePlayStatsAsync()` exists but is never called. Admin phrase stats (solve rate, calibrated difficulty, avg time to solve) are always stale/zero. Fix: call after game completion. Note: the method currently matches on `PhraseText` which may not be the PhraseUniqueId — verify and fix the query. |
| 106 | Fix admin nav link disappearing on page refresh | P1 | 30 | Done | **DEFECT**: `mapResponseToUser()` in useUser.ts maps UserResponse to User but does NOT include `roles`. On page load, `syncUser()` fetches user data from `GET /api/user/{id}` and overwrites the user object — wiping `roles` set during login. Since `isAdmin` checks `(user.roles ?? []).includes('Admin')`, admin nav link disappears. Fix requires TWO changes: (1) add `roles` field to backend `UserResponse` DTO and populate from UserRoles table, (2) update `mapResponseToUser()` to include roles. |
| 107 | Backend: Add roles to UserResponse DTO | P1 | 30 | Done | Backend `UserResponse` model has no `roles` property. The roles are only returned in `AuthResponse` during login/register. All `GET /api/user/*` endpoints return `UserResponse` without roles. Fix: add `List<string> Roles` to `UserResponse`, populate by querying `UserRoles` + `Roles` tables in `UserService` methods that return user data. |
| 108 | Consistent adminApi error handling | P2 | 31 | Done | `adminApi.ts` uses `handleResponse<T>()` for some endpoints but duplicates 401 handling inline for getUsers, getGames, getPhrases. This leads to inconsistent error handling. Fix: refactor all endpoints to use the shared `handleResponse<T>()` helper. |
| 109 | AdminGuard should verify admin role, not just token existence | P2 | 31 | Done | `AdminGuard.tsx` only checks `getAdminToken() !== null`. A non-admin user who somehow has a JWT token (e.g., from normal login that stores to wrong key) would pass the guard. Fix: decode JWT to check for admin role claim, or add an `/api/auth/verify-admin` endpoint. |
| 110 | Admin player analytics show empty when PlayerStats row missing | P2 | 31 | Done | `AdminService.GetPlayerAnalyticsAsync()` returns `null` when no `PlayerStats` row exists (new user, never recomputed). Frontend shows empty analytics section with no message. Fix: when null, either return a zero-initialized PlayerStats object OR show "No games played yet" in the frontend. |
| 111 | Trigger ClueEffectiveness recomputation | P3 | 31 | Done | `AnalyticsService` has no `RecomputeClueEffectivenessAsync()` implementation even though the `ClueEffectiveness` table exists. Need to: (1) implement the recompute method that aggregates from GameEvents (clue events + subsequent guess results), (2) call it after game completion or on a scheduled basis. Without this, clue quality analytics are permanently empty. |
| 112 | Admin games detail shows 0 events | P2 | 30 | Done | **DEFECT** (downstream of #103): `GamesManagerService.GetGameEventsAsync()` queries `GameEvents` table, which is always empty because events are never persisted. Once #103 is fixed, this will self-resolve. However, also need to verify the admin UI correctly renders the event timeline (clue/guess/gameend types with proper fields). |
| 113 | PhrasePlayStats query uses wrong field | P2 | 30 | Done | **DEFECT**: `AnalyticsService.RecomputePhrasePlayStatsAsync()` filters by `g.PhraseText == phraseUniqueId` — comparing the full phrase text string against a UniqueId. Should filter by PhraseId or by the actual phrase unique ID field on GameRecord. This means phrase stats will NEVER match any games. Fix: add `PhraseUniqueId` to GameRecord or fix the query to match correctly. |

### Auth & Session Management

| # | Item | Priority | Sprint | Status | Notes |
|---|------|----------|--------|--------|-------|
| 116 | BUG: Admin menu not visible for logged-in admin user | P1 | 38 | | **DEFECT**: AuthContext does not implement automatic token refresh. JWT expires after 60 minutes; on expiry, `isAdmin` becomes `false` while user data persists in localStorage — admin appears logged in but has no admin menu. `api.refreshToken()` exists but is never called. Fix: (1) attempt token refresh in AuthContext initialization when stored token is expired, (2) attempt refresh on the expiry-check interval before clearing, (3) on refresh failure, force complete sign-out (clear auth + user localStorage). |
| 117 | BUG: Roles stripped from user state after profile operations | P1 | 38 | | **DEFECT**: `UserController.UpdateUser()`, `UpdateDifficulty()`, `AddPoints()`, and `GetAllUsers()` call `MapToResponse()` without roles. After any profile operation, the frontend receives a `UserResponse` with empty `Roles`, overwriting the user object and losing admin status. Fix: use `MapToResponseWithRolesAsync()` (or pass roles) in all endpoints that return `UserResponse`. |

### Game Persistence Reliability

_Source: Full-stack persistence gap analysis (2026-04-11). Critical gaps where game data is lost, partially saved, or never recorded._

| # | Item | Priority | Sprint | Status | Notes |
|---|------|----------|--------|--------|-------|
| 118 | Await game record persistence (fix fire-and-forget) | P1 | 39 | | **CRITICAL**: `PersistGameRecordAsync` is called with `_ = PersistGameRecordAsync(session)` (not awaited) in both `SubmitGuess` and `GiveUp`. If the DB is down or the save fails, the frontend receives a success response but the game is never recorded. Fix: make `SubmitGuess` and `GiveUp` async, await persistence, and return error status to frontend if save fails. |
| 119 | Wrap GameRecord + GameEvents save in UnitOfWork transaction | P1 | 39 | | **CRITICAL**: `PersistGameRecordAsync` saves GameRecord and GameEvents in two separate `SaveChangesAsync` calls. If step 1 succeeds but step 2 fails, a GameRecord exists with no events (orphaned). Fix: wrap both saves in `IUnitOfWork.BeginTransactionAsync`/`CommitTransactionAsync` for atomicity. |
| 120 | Load GameEvents when reading GameRecords from DB | P1 | 39 | | `EfGameRecordRepository.GetByGameIdAsync()` and `GetByUserIdAsync()` return GameRecords with empty Events list because `entity.Ignore(e => e.Events)` prevents EF Include. Fix: add explicit join query or separate events loading method. Required for game history, admin game detail, and API game record endpoint. |
| 121 | Persist GameRecord to DB at game start (not just on completion) | P1 | 40 | | GameRecord is created in memory at game start (`Result = InProgress`) but only inserted into DB when game ends. If the server crashes or session expires, no record of the game exists. Fix: save GameRecord to DB immediately on game start, then update on completion. |
| 122 | Persist game events incrementally (not batch at end) | P1 | 40 | | Clue and guess events accumulate in the in-memory session and are only written to DB when the game ends. A crash mid-game loses all events. Fix: persist each event to the `GameEvents` table as it occurs, not as a batch at game end. |
| 123 | Track abandoned/expired games in DB | P2 | 40 | | `SessionCleanupService` removes expired sessions from memory with no DB record. Fix: before removing an expired session with a GameRecord, set `Result = Abandoned`, `CompletedAt = now`, and persist to DB. Add `Abandoned` to `GameResult` enum. |
| 124 | Make GameService methods async end-to-end | P1 | 39 | | `SubmitGuess` and `GiveUp` are synchronous methods that fire-and-forget async persistence. Callers (`GameController`) already have async signatures but discard Tasks. Fix: make `SubmitGuess`/`GiveUp` return `Task<>`, await persistence inside, propagate errors to controller for proper HTTP error responses. |
| 125 | Add persistence failure response to frontend | P2 | 41 | Done | Frontend has no concept of "game completed but save failed". After fixing fire-and-forget (#118), update `GuessResponse` and `GameState` DTOs to include `persistenceStatus` field (saved/failed/pending). Frontend can show warning if game wasn't saved. |
| 126 | Game history API endpoint with events | P2 | 41 | Done | `GET /api/user/{id}/games` returns GameRecords without events. `GET /api/game/{sessionId}/record` only works for active sessions. Add `GET /api/game/{gameId}/detail` endpoint that loads GameRecord + GameEvents from DB for any completed game. Frontend game history drill-down needs this. |
| 127 | Populate GameSessions table for session recovery | P3 | 41 | Done | `GameSessions` table exists but is never written to. Persist session state (RevealedWords, scores, clue/guess counts) to this table on each state change. On server restart, reload active sessions from DB. Complements in-memory `InMemorySessionStore` as a write-through cache. |
| 128 | Backend tests for game persistence paths | P1 | 39 | | No tests cover the persistence code paths in `PersistGameRecordAsync`. Add tests: (1) successful persist with events, (2) transaction rollback on partial failure, (3) analytics recompute after persist, (4) guest session skips persistence. |

### Leaderboard Data Quality

| # | Item | Priority | Sprint | Status | Notes |
|---|------|----------|--------|--------|-------|
| 129 | Leaderboard shows only real player data from DB | P1 | 42 | Done | **DEFECT**: `GetLeaderboardAsync` calls `GetAllAsync()` which returns ALL active users including simulated (SIM-prefix) users. Leaderboard must exclude simulated users. Additionally, the controller uses N+1 `GetGameCountAsync` calls per user instead of joining with `PlayerStats` table. Fix: (1) filter out `IsSimulated` users in `GetLeaderboardAsync`, (2) use `PlayerStats` join for GamesPlayed/GamesSolved/BestScore/CurrentStreak data, (3) update `LeaderboardEntry` model to include richer stats, (4) update frontend to display real DB data with expanded columns. |

### Advanced Linguistic Features

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 30 | POS tagging integration | P3 | 11 | Replace stop-word list with real POS tagger for phrase decomposition. Consider spaCy API or cloud NLP service. |
| 31 | Reinforcement learning (LinUCB) | P4 | 11 | Contextual bandit for clue selection. Requires `ClueEffectiveness` data (#52) with sufficient volume. Feature vector per system doc 07. Store learned weights in MySQL. |

### Social & Community (Future)

_Source: Wireframes "4 - Society.docx", ER diagram "Users, Societies & Roles", class diagram "Users, Players, Admin, Society"._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 32 | Share game results | P4 | 12+ | Social sharing cards, copy-to-clipboard results format (Wordle-style grid). No database changes needed. |
| 33 | Societies/groups tables & API | P4 | 12+ | Source: ER diagram "Users, Societies & Roles". `Societies` table: `Id INT AUTO_INCREMENT PK`, `Name VARCHAR(100) NOT NULL UNIQUE`, `Description TEXT NULL`, `CreatedBy VARCHAR(30)` (FK → Users), `IsActive BIT DEFAULT 1`, `CreatedAt DATETIME(3)`. `SocietyMembers` junction: `SocietyId INT` (FK), `UserId VARCHAR(30)` (FK), `Role VARCHAR(20) DEFAULT 'Member'` (Member / Officer / Owner), `JoinedAt DATETIME(3)`, composite PK. API: CRUD societies, join/leave, member list, transfer ownership. |
| 34 | Society leaderboards & activity feed | P4 | 12+ | Per-society leaderboard ranked by `PlayerStats.LifetimePoints` within group. Activity feed: recent games/achievements by members (query `GameRecords + SocietyMembers` join). Source: wireframe "4 - Society". |
| 61 | Community phrase submission | P4 | 12+ | `POST /api/phrases/submit` for registered users. Creates GamePhrase with `Status = Pending` + PhraseReview record (#51). Rate-limited (max 5/day per user). Moderators/Admins review via Games Manager (#59). |
| 62 | Player achievements & badges | P4 | 12+ | `Achievements` table: `Id INT AUTO_INCREMENT PK`, `Name VARCHAR(100) NOT NULL`, `Description TEXT`, `CriteriaType VARCHAR(50)` (GamesPlayed / ScoreReached / StreakReached / etc.), `CriteriaValue INT`, `BadgeIcon VARCHAR(100) NULL`. `UserAchievements` junction: `UserId VARCHAR(30)` (FK), `AchievementId INT` (FK), `EarnedAt DATETIME(3)`, composite PK. Checked on game completion. Seed: First Win, 10-Game Streak, 1000 Points, Speed Demon (under 60s), Perfectionist (no wrong guesses). |
| 35 | Admin panel (full) | P4 | 12+ | Complete admin panel integrating: user management (#57), phrase management (#59), site config (#60), analytics dashboards (#28), audit log viewer (#69), society moderation. |

---

## Sprint Roadmap

Sprints are sequenced by dependencies and priority. Each sprint builds on the previous one. Estimated scope: 1-2 weeks each.

| Sprint | Theme | Key Items | Dependencies |
|--------|-------|-----------|--------------|
| **30** | **Admin Critical Data Fixes** | #103, #104, #105, #106, #107, #112, #113 | Sprint 29 (admin UI) |
| **31** | **Admin Quality & Completeness** | #108, #109, #110, #111 | Sprint 30 (data fixes) |
| **39** | **Reliable Game Completion Persistence** | #118, #119, #120, #124, #128 | Sprint 38 |
| **40** | **In-Progress Game Persistence** | #121, #122, #123 | Sprint 39 (reliable persistence) |
| **41** | **Game Data Completeness & Frontend Integration** | #125, #126, #127 | Sprint 40 (incremental persistence) |
| **1** | **Testing & API Foundation** | #1, #2, #3 | None — must be first |
| **2** | **Difficulty & Scoring Engine** | #4, #5, #6, #7 | Sprint 1 (tests) |
| **3** | **Frontend Architecture & History** | #8, #9, #10 | Sprint 1 (tests) |
| **4** | **Responsive, CI/CD, Infrastructure** | #11, #15, #16, #17 | Sprint 1 (tests), Sprint 3 (routing) |
| **5** | **Clue Quality & Linguistic Engine** | #12, #13, #14 | Sprint 2 (difficulty system) |
| **6** | **Engagement & Accessibility** | #18, #19, #20, #21, #37-#42 | Sprint 3 (routing), Sprint 2 (scoring) |
| **7** | **Security & Authentication** | #22, #23, #24 | Sprint 1 (tests), Sprint 3 (routing) |
| **8a** | **MySQL Infrastructure & Core Tables** | #25, #26, #43, #44, #63, #64 | Sprint 7 (auth), Sprint 4 (infra) |
| **8b** | **Game Data Normalization** | #45, #46, #65, #66 | Sprint 8a (EF Core + repositories) |
| **8c** | **Session Persistence & Operations** | #47, #67, #68 | Sprint 8b (normalized schema) |
| **8d** | **Authorization & Roles** | #48, #69 | Sprint 7 (JWT), Sprint 8a (MySQL) |
| **9a** | **Content Management Schema** | #49, #50, #51 | Sprint 8b (game data tables) |
| **9b** | **Analytics Computed Tables** | #27, #29, #52, #53, #54 | Sprint 8b (GameRecords + GameEvents) |

---

## Retrospective Recommendations (Process & Documentation Improvements)

_Source: Comprehensive scan of all 18 sprint retrospective files (Sprint 1–33). These are process improvements — updates to agent instructions, documentation, and workflow — not feature work. Each item traces back to the sprint(s) that identified the issue._

### Priority Definitions

| Priority | Meaning |
|----------|---------|
| CRITICAL | Actively causing bugs or build surprises; should be addressed next sprint |
| HIGH | Recurring pattern across multiple sprints; plan within next 2 sprints |
| MEDIUM | Would improve quality/efficiency; schedule when capacity allows |

### CRITICAL — Address Next Sprint

| # | Item | Priority | Source Sprint(s) | Target Document | Notes |
|---|------|----------|-----------------|-----------------|-------|
| R1 | Establish `npm run build` as definitive frontend build check (not `tsc --noEmit`) | CRITICAL | 33 | `.github/skills/full-test/SKILL.md`, `.github/copilot-instructions.md` | `tsc --noEmit` misses unused imports and stricter errors that `tsc -b` (used by `npm run build`) catches. Update the full-test skill and copilot instructions to always use `npm run build` as the build verification step. |
| R2 | Add "Test Coverage Requirements" to sprint plan template | CRITICAL | 28, 29, 33 | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` Phase 1 | Recurring pattern: new code ships without tests. Sprint plans that say "update tests" must specify WHAT tests (e.g., "Add 3 unit tests for AuthContext login/logout/token-restore"). Add a mandatory "Test Expectations" section to the sprint plan template. |

### HIGH — Plan Within Next 2 Sprints

| # | Item | Priority | Source Sprint(s) | Target Document | Notes |
|---|------|----------|-----------------|-----------------|-------|
| R3 | Document "Grep-Before-Delete" developer practice | HIGH | 13 | `.github/copilot-instructions.md` | When removing a property, function, or file, ALWAYS grep the entire codebase first. Sprint 13 learned this the hard way when property removal broke files that weren't checked. Add a "Refactoring Checklist" section to copilot-instructions. |
| R4 | Create "API Integration Checklist" for frontend work | HIGH | 28 | `.github/copilot-instructions.md` | Sprint 28 had API signature mismatches. Before calling any backend endpoint from frontend: verify parameter order, types, and defaults. Add a checklist to the Frontend Patterns section. |
| R5 | Add Sprint Planning Pre-Audit step to Phase 0 | HIGH | 3, 9 | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` Phase 0 | Sprints 3 and 9 discovered backlog items already done or stale mappings. Add a mandatory pre-planning audit: verify all candidate items are still relevant, check if any are already implemented, update sprint mappings. Sprint 9's research step saved 50% effort. |
| R6 | Session-spanning sprint checkpoint protocol | HIGH | 33 | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` | When a sprint exhausts context mid-execution, save task-level progress to `sprint-status.json` (which tasks done, which in-progress, which pending) and commit WIP. Document the resume protocol. |

### MEDIUM — Schedule When Capacity Allows

| # | Item | Priority | Source Sprint(s) | Target Document | Notes |
|---|------|----------|-----------------|-----------------|-------|
| R7 | Add "Frontend Refactor: Early Build Validation" guideline | MEDIUM | 33 | `.github/copilot-instructions.md` | During major frontend refactors, run `npm run build` after the first significant file change (not just at the end). Catches cascading issues early. |
| R8 | Update backlog sprint mappings immediately on scope changes | MEDIUM | 3 | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` Phase 1 | When a sprint plan changes scope (items added/removed), update BACKLOG.md sprint column immediately to prevent downstream confusion. |
| R9 | Document reflection-based testing fragility | MEDIUM | 6 | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` or testing guide | When tests use reflection to access private helpers, they're fragile to refactoring. Document when to use reflection vs. testing through public interfaces. |
| R10 | Script validation checklist (port checks, error handling) | MEDIUM | 2 | `.github/copilot-instructions.md` → Scripts section | Scripts that manage services (start-app.bat, stop-app.bat) need pre-flight checks: port availability, process existence, graceful error messages. Document as a checklist for future scripts. |
| R11 | Formalize pre-sprint research as mandatory Phase 0 step | MEDIUM | 9 | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` Phase 0 | Sprint 9 showed research (checking what's already done, validating assumptions) saves significant effort. Make it a repeatable, documented sub-step. |
| R12 | Component architecture evolution guidelines | MEDIUM | 7, 27, 29 | `.github/copilot-instructions.md` → Frontend Patterns | Track growing complexity signals: `useUser` return type expanding (Sprint 27), dual headers (Sprint 7), inline styles proliferating (Sprint 29). Document refactor triggers (e.g., "when a hook returns >8 values, extract an interface or split"). |

### Recurring Themes Summary

| Theme | Occurrences | Impact | Key Items |
|-------|-------------|--------|-----------|
| **Testing gaps** — new code ships without tests | Sprints 28, 29, 33 | Untested code paths accumulate | R2 |
| **API integration mismatches** | Sprints 10, 28 | Frontend calls fail at runtime | R4 |
| **Build check inconsistency** | Sprint 33 | Unused imports slip to production build | R1 |
| **Backlog accuracy drift** | Sprints 3, 9 | Wasted planning time, stale items | R5, R8 |
| **Context budget exhaustion** | Sprint 33 | Session breaks, lost intermediate state | R6 |
| **Developer practice gaps** | Sprints 2, 13, 33 | Bugs from missing grep, missing checks | R3, R7, R10 |
| **Growing technical complexity** | Sprints 7, 27, 29 | Maintainability declining | R12 |
| **9c** | **Simulated Computer Player** | #70, #71, #72, #73, #74, #75, #76 | Sprint 8b (game data), Sprint 9b (analytics tables) |
| **10** | **Admin, Games Manager & Data Explorer** | #28, #56, #57, #58, #59, #60, #77, #78, #79, #80, #81 | Sprint 8d (roles), Sprint 9a-9c (all schemas + simulation) |
| **11** | **Advanced NLP & RL** | #30, #31 | Sprint 9b (analytics data), Sprint 5 (xnym) |
| **12+** | **Social & Community** | #32, #33, #34, #35, #61, #62 | Sprint 7 (auth), Sprint 8b (MySQL) |

### Sprint Dependency Graph

```
Sprint 1 (Tests + API) ────────┬──────────────────────────────────────┐
       │                       │                                      │
       ▼                       ▼                                      ▼
Sprint 2 (Difficulty)    Sprint 3 (Routing)                    Sprint 7 (Auth)
       │                    │       │                              │
       ▼                    ▼       ▼                              ▼
Sprint 5 (Clue Quality) Sprint 4 (Responsive/CI)     Sprint 8a (MySQL Infrastructure)
       │                    │                              │
       ▼                    ▼                              ▼
Sprint 6 (Engagement) ◄────┘                    Sprint 8b (Game Data Normalization)
                                                   │              │
                                                   ▼              ▼
                                          Sprint 8c (Sessions) Sprint 8d (Roles)
                                                   │              │
                                                   ▼              ▼
                                          Sprint 9a (Content)  Sprint 9b (Analytics)
                                                   │              │
                                                   │              ▼
                                                   │       Sprint 9c (Simulation)
                                                   │              │
                                                   └──────┬───────┘
                                                          ▼
                                               Sprint 10 (Admin & Data Explorer)
                                                          │
                                                          ▼
                                                  Sprint 11 (NLP/RL)
                                                          │
                                                          ▼
                                                  Sprint 12+ (Social)
```

### Sprint 8 Sub-Sprint Rationale

Sprint 8 is split into 4 sub-sprints (8a-8d) because the MySQL migration is too large for a single sprint and has internal dependencies:

- **8a** establishes EF Core infrastructure, refactors DI lifetimes, and creates core tables (Users, GamePhrases). This is the riskiest phase — validates the entire migration approach.
- **8b** normalizes game data (GameRecords, GameEvents) which requires the User model refactoring from 8a. The JSON → MySQL migration tool runs here because it needs all tables to exist.
- **8c** adds operational features (session persistence, health check, seeding) that build on the normalized schema.
- **8d** adds roles/authorization tables that extend the JWT system from Sprint 7 and require MySQL from 8a.

Each sub-sprint is independently deployable. If the migration is paused after 8a, the system still works (feature flag falls back to JSON).

---

## Completed Items

_Move items here after sprint completion. Include sprint number._

| Item | Sprint | Date |
|------|--------|------|
| #1 Backend test suite (xUnit) | 1 | 2026-04-05 |
| #2 Frontend test suite (Vitest + Testing Library) | 1 | 2026-04-05 |
| #3 API response standardization | 1 | 2026-04-05 |
| #36 One-click app launcher | 2 | 2026-04-05 |
| #4 Difficulty-aware clue selection | 3 | 2026-04-09 |
| #5 Enhanced scoring model | 3 | 2026-04-09 |
| #6 Phrase difficulty computation | 3 | 2026-04-09 |
| #8 React Router + page structure | 4 | 2026-04-09 |
| #9 Home page | 4 | 2026-04-09 |
| #10 Game history UI | 4 | 2026-04-09 |
| #11 Responsive mobile layout | 5 | 2026-04-09 |
| #15 CI/CD pipeline (GitHub Actions) | 5 | 2026-04-09 |
| #16 Session TTL and cleanup | 5 | 2026-04-09 |
| #17 Health check improvements | 5 | 2026-04-09 |
| #12 Xnym taxonomy expansion | 6 | 2026-04-09 |
| #13 Contextual synonym selection | 6 | 2026-04-09 |
| #14 Clue caching layer | 6 | 2026-04-09 |
| #18 Leaderboard page | 7 | 2026-04-09 |
| #19 Timer and streak mechanics | 7 | 2026-04-09 |
| #21 Accessibility improvements | 7 | 2026-04-09 |
| #23 Input sanitization audit | 8 | 2026-04-09 |
| #24 Rate limiting | 8 | 2026-04-09 |
| #37 Remove splash/click-to-start screens | 9 | 2026-04-09 |
| #38 Professional game layout structure | 9 | 2026-04-09 |
| #39 Compact phrase bar with action buttons | 9 | 2026-04-09 |
| #40 Tabbed clue display as primary content | 9 | 2026-04-09 |
| #41 App footer with version and status | 9 | 2026-04-09 |
| #42 Default route to /play | 9 | 2026-04-09 |
| #22 User authentication (JWT) | 10 | 2026-04-09 |
| #25 EF Core infrastructure with MySQL (Pomelo) | 11 | 2026-04-09 |
| #26 Repository interfaces refactoring (IGameRecordRepository, IUnitOfWork) | 11 | 2026-04-09 |
| #64 Data provider feature flag (Json/MySql) | 11 | 2026-04-09 |
| #63 DI lifetime migration (Scoped-compatible services) | 12 | 2026-04-09 |
| #45 GameRecords table + IGameRecordRepository | 12 | 2026-04-09 |
| #46 GameEvents table (single-table inheritance) | 12 | 2026-04-09 |
| #65 User model refactoring (decouple Games) | 13 | 2026-04-09 |
| #67 MySQL health check endpoint | 14 | 2026-04-09 |
| #68 Database seeding (admin user, test data) | 14 | 2026-04-09 |
| #47 Session persistence (DB-backed) | 15 | 2026-04-09 |
| #66 JSON → MySQL data migration tool | 15 | 2026-04-09 |
| #48 Roles & authorization tables | 16 | 2026-04-09 |
| #69 Audit log table | 16 | 2026-04-09 |
| #49 Site configuration table | 17 | 2026-04-09 |
| #50 Phrase categories & tagging | 17 | 2026-04-09 |
| #51 Phrase review workflow | 17 | 2026-04-09 |
| #52 Clue effectiveness materialized table | 18 | 2026-04-09 |
| #53 Player statistics computed table | 18 | 2026-04-09 |
| #54 Phrase play statistics table | 18 | 2026-04-09 |
| #27 Clue effectiveness tracking service | 19 | 2026-04-10 |
| #29 Phrase difficulty auto-calibration | 19 | 2026-04-10 |
| #70 IsSimulated flag on data models | 20 | 2026-04-10 |
| #71 Simulated user ID format (SIM- prefix) | 20 | 2026-04-10 |
| #72 Simulation behavior profiles | 20 | 2026-04-10 |
| #73 ISimulationService + game simulation engine | 21 | 2026-04-10 |
| #74 Batch simulation runner | 21 | 2026-04-10 |
| #75 Simulation data purge tool | 21 | 2026-04-10 |
| #56 Admin dashboard API | 22 | 2026-04-10 |
| #57 Admin user management UI | 22 | 2026-04-10 |
| #78 Player detail view API & UI | 22 | 2026-04-10 |
| #58 Games Manager API | 23 | 2026-04-10 |
| #59 Games Manager UI | 23 | 2026-04-10 |
| #60 Site configuration admin UI | 23 | 2026-04-10 |
| #79 Game detail view API & UI | 23 | 2026-04-10 |
| #80 Game browser API & UI | 23 | 2026-04-10 |
| #81 Data summary & system overview | 24 | 2026-04-10 |
| #77 Simulation summary dashboard | 24 | 2026-04-10 |
| #82 Apply InitialCreate EF Core migration | 25 | 2026-04-10 |
| #83 Run JSON-to-MySQL data migration | 25 | 2026-04-10 |
| #84 Update admin user seed with credentials | 25 | 2026-04-10 |
| #85 Switch DataProvider to MySql | 25 | 2026-04-10 |
| #86 Admin login page | 26 | 2026-04-10 |
| #87 Admin route guards and layout | 26 | 2026-04-10 |
| #88 Admin dashboard page | 26 | 2026-04-10 |
| #89 Admin user management page | 26 | 2026-04-10 |
| #90 Admin games manager page | 26 | 2026-04-10 |
| #91 Admin site config page | 26 | 2026-04-10 |
| #92 Admin data explorer page | 26 | 2026-04-10 |
| #70a Admin nav link for admin users | 27 | 2026-04-10 |
| #93 Fix frontend TypeScript build errors | 28 | 2026-04-10 |
| #94 Admin Phrases Management page | 28 | 2026-04-10 |
| #95 Admin user search and filter | 28 | 2026-04-10 |
| #96 Admin user role management | 28 | 2026-04-10 |
| #102 Admin phrase CRUD backend endpoints | 28 | 2026-04-10 |
| #97 Confirmation dialogs for destructive actions | 29 | 2026-04-10 |
| #98 Admin audit log viewer | 29 | 2026-04-10 |
| #99 Admin CSS cleanup (extract inline styles) | 29 | 2026-04-10 |
| #100 Dashboard refresh and date filters | 29 | 2026-04-10 |
| #103 Fix GameEvent persistence | 30 | 2026-04-10 |
| #104 Recompute PlayerStats after game completion | 30 | 2026-04-10 |
| #105 Recompute PhrasePlayStats after game completion | 30 | 2026-04-10 |
| #106 Fix admin nav link disappearing on refresh | 30 | 2026-04-10 |
| #107 Backend: Add roles to UserResponse DTO | 30 | 2026-04-10 |
| #108 Consistent adminApi error handling | 31 | 2026-04-10 |
| #109 AdminGuard should verify admin role | 31 | 2026-04-10 |
| #110 Admin player analytics empty state handling | 31 | 2026-04-10 |
| #111 Trigger ClueEffectiveness recomputation | 31 | 2026-04-10 |
| #112 BUG: PhraseUniqueId missing column migration | 32 | 2026-04-10 |
| #113 BUG: Admin re-login (dual-token system) | 33 | 2026-04-10 |
| #114 Introduce AuthContext for centralized auth | 33 | 2026-04-10 |
| #115 Remove redundant admin login page | 33 | 2026-04-10 |
