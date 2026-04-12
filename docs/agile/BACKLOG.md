# LinkittyDo Backlog

Master backlog of all planned work for LinkittyDo. Items are prioritized and grouped by category. This is the single source of truth for what to build next.

**Last Updated**: 2026-04-12
**Source Analysis**: See [DESIGN_CONTENT_ANALYSIS.md](DESIGN_CONTENT_ANALYSIS.md) for the full gap assessment that generated this backlog.

---

## How to Use This Document

- **Sprint planning**: Select items from the top of each priority group
- **After each sprint**: Remove completed items, add new discoveries, re-prioritize
- **Adding items**: Add to the appropriate category with a priority level

### Priority Levels

| Priority | Meaning |
|----------|---------|
| P1 - Critical | Must be done next; blocks other work or core functionality |
| P2 - High | Important for the product; should be in an upcoming sprint |
| P3 - Medium | Valuable but not urgent; schedule when capacity allows |
| P4 - Low | Nice to have; do when convenient |

---

## Backlog Items

### Core Gameplay Enhancement

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 7 | Scale phrase database | P2 | — | Only 26 phrases exist. Batch-generate 100+ phrases across difficulty bands. Add duplicate detection. |

### User Experience & Engagement

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 20 | Sound effects polish | P3 | — | Audio assets exist but integration is basic. Add difficulty-appropriate audio cues, volume control, mute toggle. |

### Data Layer

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 43 | Users table + EF Core repository | P2 | — | `Users` table: `UniqueId VARCHAR(30) PK`, `Name VARCHAR(50) NOT NULL`, `Email VARCHAR(255) NOT NULL`, `LifetimePoints INT DEFAULT 0`, `PreferredDifficulty INT DEFAULT 10`, `IsActive BIT DEFAULT 1`, `CreatedAt DATETIME(3) NOT NULL`, `UpdatedAt DATETIME(3) NULL`, `DeletedAt DATETIME(3) NULL`. Unique indexes on `Name` (case-insensitive collation) and `Email`. Implement `EfUserRepository : IUserRepository`. |
| 44 | GamePhrases table + EF Core repository | P2 | — | `GamePhrases` table: `UniqueId VARCHAR(30) PK`, `Text VARCHAR(500) NOT NULL UNIQUE`, `WordCount INT NOT NULL`, `Difficulty INT DEFAULT 0`, `GeneratedByLlm BIT DEFAULT 0`, `IsActive BIT DEFAULT 1`, `CreatedAt DATETIME(3) NOT NULL`. Implement `EfGamePhraseRepository : IGamePhraseRepository`. |

### Analytics & Intelligence

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 28 | Player analytics dashboard | P3 | — | Frontend dashboard showing aggregated player behavior from `PlayerStats` (#53): play frequency, difficulty progression over time, solve rates. Admin-only initially; later expose per-user stats on profile page. |
| 76 | Analytics dual-mode (real vs simulated vs combined) | P3 | — | Update all analytics computed tables (#52, #53, #54) and their recompute jobs to support three modes: `real-only` (exclude IsSimulated), `simulated-only`, `combined`. Admin dashboard (#56) exposes a toggle control. Default for player-facing views (leaderboard, profile): real-only. Default for admin analytics: combined. Stored as `SiteConfig` parameter. |

### Auth & Session Management

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 116 | BUG: Admin menu not visible for logged-in admin user | P1 | — | **DEFECT**: AuthContext does not implement automatic token refresh. JWT expires after 60 minutes; on expiry, `isAdmin` becomes `false` while user data persists in localStorage — admin appears logged in but has no admin menu. `api.refreshToken()` exists but is never called. Fix: (1) attempt token refresh in AuthContext initialization when stored token is expired, (2) attempt refresh on the expiry-check interval before clearing, (3) on refresh failure, force complete sign-out (clear auth + user localStorage). |
| 117 | BUG: Roles stripped from user state after profile operations | P1 | — | **DEFECT**: `UserController.UpdateUser()`, `UpdateDifficulty()`, `AddPoints()`, and `GetAllUsers()` call `MapToResponse()` without roles. After any profile operation, the frontend receives a `UserResponse` with empty `Roles`, overwriting the user object and losing admin status. Fix: use `MapToResponseWithRolesAsync()` (or pass roles) in all endpoints that return `UserResponse`. |

### Game Persistence Reliability

_Critical gaps where game data is lost, partially saved, or never recorded._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 118 | Await game record persistence (fix fire-and-forget) | P1 | — | **CRITICAL**: `PersistGameRecordAsync` is called with `_ = PersistGameRecordAsync(session)` (not awaited) in both `SubmitGuess` and `GiveUp`. If the DB is down or the save fails, the frontend receives a success response but the game is never recorded. Fix: make `SubmitGuess` and `GiveUp` async, await persistence, and return error status to frontend if save fails. |
| 119 | Wrap GameRecord + GameEvents save in UnitOfWork transaction | P1 | — | **CRITICAL**: `PersistGameRecordAsync` saves GameRecord and GameEvents in two separate `SaveChangesAsync` calls. If step 1 succeeds but step 2 fails, a GameRecord exists with no events (orphaned). Fix: wrap both saves in `IUnitOfWork.BeginTransactionAsync`/`CommitTransactionAsync` for atomicity. |
| 120 | Load GameEvents when reading GameRecords from DB | P1 | — | `EfGameRecordRepository.GetByGameIdAsync()` and `GetByUserIdAsync()` return GameRecords with empty Events list because `entity.Ignore(e => e.Events)` prevents EF Include. Fix: add explicit join query or separate events loading method. Required for game history, admin game detail, and API game record endpoint. |
| 121 | Persist GameRecord to DB at game start (not just on completion) | P1 | — | GameRecord is created in memory at game start (`Result = InProgress`) but only inserted into DB when game ends. If the server crashes or session expires, no record of the game exists. Fix: save GameRecord to DB immediately on game start, then update on completion. |
| 122 | Persist game events incrementally (not batch at end) | P1 | — | Clue and guess events accumulate in the in-memory session and are only written to DB when the game ends. A crash mid-game loses all events. Fix: persist each event to the `GameEvents` table as it occurs, not as a batch at game end. |
| 123 | Track abandoned/expired games in DB | P2 | — | `SessionCleanupService` removes expired sessions from memory with no DB record. Fix: before removing an expired session with a GameRecord, set `Result = Abandoned`, `CompletedAt = now`, and persist to DB. Add `Abandoned` to `GameResult` enum. |
| 124 | Make GameService methods async end-to-end | P1 | — | `SubmitGuess` and `GiveUp` are synchronous methods that fire-and-forget async persistence. Callers (`GameController`) already have async signatures but discard Tasks. Fix: make `SubmitGuess`/`GiveUp` return `Task<>`, await persistence inside, propagate errors to controller for proper HTTP error responses. |
| 128 | Backend tests for game persistence paths | P1 | — | No tests cover the persistence code paths in `PersistGameRecordAsync`. Add tests: (1) successful persist with events, (2) transaction rollback on partial failure, (3) analytics recompute after persist, (4) guest session skips persistence. |

### Bug Fixes

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 129 | BUG: Admin navigation persists after switching to non-admin user | P1 | — | When an admin user switches to a non-admin user (e.g., Tom), the Admin nav link and admin page access persist because `switchUser` only updates the displayed user profile via `api.getUser()` without re-authenticating. The JWT (with admin role) stays in AuthContext, so `isAdmin` remains true. Fix: `switchUser` must clear auth state (logout JWT) when switching users, forcing `isAdmin` to derive from the switched user's actual roles, not a stale JWT. Also: when current user lacks admin role, force-redirect away from admin pages and hide admin nav. |
| 130 | BUG: Leaderboard shows rows without real player stats and names not displaying | P1 | — | Leaderboard shows all active non-simulated users regardless of whether they have actually played a game. Users with 0 games, 0 points appear with blank stats. Additionally, player names may not display correctly. Fix: (1) Backend — filter leaderboard to only include users with `GamesPlayed > 0` or `LifetimePoints > 0`, (2) Frontend — remove any canned/prepopulated rows, (3) Verify the Name column is populated correctly from the database, (4) Clarify Rank column (sequential 1-based index, medals for top 3). |

### Admin Features

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 101 | Admin data export (CSV) | P3 | — | No export capability for user lists, game stats, or config. Add CSV export buttons to AdminUsers and AdminGames pages. |
| 131 | Admin hard-delete user | P1 | — | Admin ability to permanently delete a user and ALL related data (GameRecords, GameEvents, GameSessions, UserRoles, PlayerStats, AuditLog entries). Uses transactional delete to maintain data integrity. Adds `DELETE /api/admin/users/{uniqueId}` endpoint with `[Authorize(Policy = "RequireAdmin")]`. Must delete in correct FK order to avoid constraint violations. |
| 132 | Games Manager: show player name and hide Game ID | P1 | — | Game list table shows Game ID (truncated) which is useless to admins. Replace with player name by joining GameRecords with Users table. Backend: update SearchGames endpoint to include `playerName`. Frontend: replace Game ID column with Player column. |
| 133 | Games Manager: rich event detail view | P1 | — | Game detail events only show type and timestamp. Expand to show: clue events with URL link, phrase word, search term; guess events with word, guess text, correct/incorrect badge, points; game end with reason. Backend: return full polymorphic event data from GetGameDetail. Frontend: render rich event rows. |
| 134 | Add RelationshipType to ClueEvent | P1 | — | ClueEvent stores SearchTerm but not what type of relationship it has to the original word (synonym, antonym, trigger, homophone, similar). Add `RelationshipType` string property to ClueEvent, EF Core migration, update ClueService to track which Datamuse endpoint produced the selected term. |
| 135 | Games Manager: date+time formatting | P2 | — | Played column shows date only, event timestamps show time only. Both should show full date and time. |
| 136 | CI lint: setState-in-effect in UserManageModal | P1 | — | ESLint `react-hooks/set-state-in-effect` error. `useEffect` on line 35 calls `setDifficulty`/`setSelectedUserId` synchronously. Fix: initialize state from props using initializer functions or use key-based reset pattern. |
| 137 | CI lint: setState-in-effect in UserModal (4 violations) | P1 | — | ESLint `react-hooks/set-state-in-effect` errors on lines 40, 53, 85, 112. Reset effect calls multiple setState synchronously; debounced validation effects set error state synchronously in early-return paths. Fix: use key-based reset for modal open, derive validation state or use event handlers. |
| 138 | CI lint: unused `_allUsers` variable in UserModal | P1 | — | ESLint `@typescript-eslint/no-unused-vars`. `allUsers` is destructured as `_allUsers` but never used. Remove from props interface or use it. |
| 139 | CI lint: fast-refresh violation in AuthContext | P1 | — | ESLint `react-refresh/only-export-components` error on line 238. File exports both `AuthProvider` component and `useAuth` hook. Fix: move `useAuth` to a separate file or add eslint-disable comment. |

### Data Cleanup

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 140 | Remove legacy JSON data files and JSON repository code | P1 | 49 | MySQL is the active data provider. JSON files in `Data/Phrases/`, `Data/Users/`, `Data/GameRecords/` are redundant. Remove: JSON data files, JSON repository classes (`JsonUserRepository`, `JsonGamePhraseRepository`, `JsonGameRecordRepository`), JSON health check, `DataProvider` feature flag, migration service/controller, `.csproj` copy rule. Verify DB has all data first. Update affected tests. |

### Advanced Linguistic Features

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 30 | POS tagging integration | P3 | — | Replace stop-word list with real POS tagger for phrase decomposition. Consider spaCy API or cloud NLP service. |
| 31 | Reinforcement learning (LinUCB) | P4 | — | Contextual bandit for clue selection. Requires `ClueEffectiveness` data (#52) with sufficient volume. Feature vector per system doc 07. Store learned weights in MySQL. |

### Social & Community (Future)

_Source: Wireframes "4 - Society.docx", ER diagram "Users, Societies & Roles", class diagram "Users, Players, Admin, Society"._

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 32 | Share game results | P4 | — | Social sharing cards, copy-to-clipboard results format (Wordle-style grid). No database changes needed. |
| 33 | Societies/groups tables & API | P4 | — | `Societies` table: `Id INT AUTO_INCREMENT PK`, `Name VARCHAR(100) NOT NULL UNIQUE`, `Description TEXT NULL`, `CreatedBy VARCHAR(30)` (FK → Users), `IsActive BIT DEFAULT 1`, `CreatedAt DATETIME(3)`. `SocietyMembers` junction: `SocietyId INT` (FK), `UserId VARCHAR(30)` (FK), `Role VARCHAR(20) DEFAULT 'Member'` (Member / Officer / Owner), `JoinedAt DATETIME(3)`, composite PK. API: CRUD societies, join/leave, member list, transfer ownership. |
| 34 | Society leaderboards & activity feed | P4 | — | Per-society leaderboard ranked by `PlayerStats.LifetimePoints` within group. Activity feed: recent games/achievements by members (query `GameRecords + SocietyMembers` join). |
| 35 | Admin panel (full) | P4 | — | Complete admin panel integrating: user management (#57), phrase management (#59), site config (#60), analytics dashboards (#28), audit log viewer (#69), society moderation. |
| 61 | Community phrase submission | P4 | — | `POST /api/phrases/submit` for registered users. Creates GamePhrase with `Status = Pending` + PhraseReview record (#51). Rate-limited (max 5/day per user). Moderators/Admins review via Games Manager (#59). |
| 62 | Player achievements & badges | P4 | — | `Achievements` table: `Id INT AUTO_INCREMENT PK`, `Name VARCHAR(100) NOT NULL`, `Description TEXT`, `CriteriaType VARCHAR(50)` (GamesPlayed / ScoreReached / StreakReached / etc.), `CriteriaValue INT`, `BadgeIcon VARCHAR(100) NULL`. `UserAchievements` junction: `UserId VARCHAR(30)` (FK), `AchievementId INT` (FK), `EarnedAt DATETIME(3)`, composite PK. Checked on game completion. Seed: First Win, 10-Game Streak, 1000 Points, Speed Demon (under 60s), Perfectionist (no wrong guesses). |

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
