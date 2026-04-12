# Changelog

All notable changes to LinkittyDo will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### 2026-04-12
- **fix**: Sprint 52 — Profile page no longer redirects to Play when JWT expires; shows "Session Expired" prompt instead
- **fix**: History page no longer shows empty state when JWT expires; shows "Session Expired" prompt instead
- **fix**: Header points now stay in sync with server after game completion (refreshUser on solve/giveup)
- **fix**: ProfilePage.test.tsx build error (GameResult type literal cast)
- **feat**: Added `refreshUser` function to `useUser` hook for on-demand server sync
- **feat**: API error messages now include HTTP status codes for auth failure detection
- **test**: 5 new frontend tests for auth error handling and user refresh behavior

### 2026-04-12
- **feat**: Sprint 51 — Daily Challenge mode with phrase-of-the-day, leaderboard, and one-play-per-day limit
- **feat**: Interactive 6-step tutorial/onboarding flow for new players
- **feat**: Player Profile page with stats grid, streaks, solve rate, and recent games
- **feat**: Daily Challenge API endpoints (GET status, POST start, GET leaderboard)
- **feat**: Profile API endpoint with computed stats from PlayerStats and game history
- **feat**: NavHeader updated with Daily, How to Play, and Profile navigation links
- **test**: Add DailyChallengeControllerTests (7 tests), UserController profile tests (3 tests)
- **test**: Add frontend tests for TutorialPage, ProfilePage, DailyChallengePage, NavHeader updates (25 tests)
- **docs**: Update README.md with new features (Daily Challenge, Tutorial, Player Profile)

### 2026-04-12
- **feat**: Sprint 50 — Scale phrase database from 5 to 115 curated seed phrases across difficulty bands (#7)
- **feat**: Pre-compute difficulty for all seed phrases using `ComputeDifficultyFromText()` static method
- **fix**: Admin phrase creation returns 409 Conflict on duplicate text instead of raw DB error
- **chore**: Backlog cleanup — moved 30+ completed items (Sprints 38-49) to Completed section
- **test**: Add 8 PhraseScalingTests (difficulty computation, duplicate handling, difficulty range validation)
- **chore**: Sprint 49 — Remove legacy JSON data files and JSON repository code (#140)
- **chore**: Remove `DataProvider` feature flag — MySQL is now the sole data provider
- **chore**: Delete JSON repository classes (`JsonUserRepository`, `JsonGamePhraseRepository`, `JsonGameRecordRepository`)
- **chore**: Delete `JsonToMySqlMigrationService`, `MigrationController`, `JsonStorageHealthCheck`
- **chore**: Delete `NoOpRoleService`, `NoOpAuditService`, `NoOpAnalyticsService`, `InMemorySiteConfigService`
- **chore**: Remove 109 phrase JSON files, 3 user JSON files, and empty GameRecords directory
- **chore**: Remove `.csproj` JSON copy rule
- **test**: Remove 18 tests for deleted JSON/NoOp code; 321 backend tests passing
- **fix**: Sprint 48 — Fix 7 CI ESLint errors across UserManageModal, UserModal, AuthContext
- **fix**: Replace setState-in-effect patterns with state adjustment during render and onChange handlers (#136, #137)
- **fix**: Remove unused `allUsers` prop from UserModal (#138)
- **fix**: Suppress fast-refresh warning for `useAuth` co-export in AuthContext (#139)

### 2026-04-11
- **feat**: Sprint 47 — Games Manager shows player name instead of Game ID in game list (#132)
- **feat**: Rich event detail view — clue events show URL link, phrase word, search term with relationship type; guess events show result and points; game end shows reason (#133)
- **feat**: Add RelationshipType to ClueEvent tracking synonym/antonym/trigger/homophone/similar relationship to original word (#134)
- **feat**: Full date+time formatting in Played column and event timestamps (#135)
- **test**: Add 3 new GamesManager tests — player name inclusion, clue event with relationship type, guess event detail

### 2026-04-11
- **feat**: Sprint 46 — Admin hard-delete user endpoint (`DELETE /api/admin/users/{uniqueId}`) permanently removes user and all related data (#131)
- **test**: Add 4 new AdminHardDelete tests — full cascade delete, other users unaffected, nonexistent user, user with no related data

### 2026-04-11
- **fix**: Sprint 45 — Filter leaderboard to only show players with LifetimePoints > 0, excluding users who never played (#130)
- **fix**: Add tooltips to leaderboard column headers clarifying Rank, Points, Games, Solved, Best Score, and Streak
- **test**: Add 3 new leaderboard tests — exclude zero-point users (JSON fallback + EF Core), empty results when all users have zero points

### 2026-04-11
- **fix**: Sprint 44 — switchUser now clears JWT auth state, preventing admin nav from persisting when switching to non-admin users (#129)
- **fix**: AdminGuard redirects non-admin users to /play instead of showing static "Access Denied" page
- **test**: Add 3 AdminGuard tests — admin access, unauthenticated redirect, non-admin redirect

### 2026-04-11
- **fix**: Sprint 43 — Rewrite leaderboard query from GroupJoin to two-query approach fixing missing names, zero stats, and order preservation
- **fix**: Add fallback stats computation from GameRecords when PlayerStats table is empty
- **fix**: Handle empty/null player names with "(unknown)" fallback in both backend and frontend
- **test**: Add 6 new EF Core leaderboard tests — stats from PlayerStats, fallback from GameRecords, empty names, simulated/inactive exclusion, order preservation

### 2026-04-11
- **feat**: Sprint 42 — Leaderboard excludes simulated users; shows only real player data from DB (#129)
- **feat**: Leaderboard uses PlayerStats join for GamesPlayed, GamesSolved, BestScore, CurrentStreak (eliminates N+1 queries)
- **feat**: Frontend leaderboard table displays Solved, Best Score, and Streak columns
- **test**: Add 3 new backend tests — simulated user exclusion, fallback stats, fallback simulated exclusion; update controller test

### 2026-04-12
- **feat**: Sprint 41 — Add PersistenceStatus enum (Saved/Failed/NotApplicable) to game responses; frontend toast on save failures (#125)
- **feat**: Lazy-load game events in GameHistoryPage via getGameDetail API; expandable event timeline with loading states (#126)
- **feat**: Rewrite DatabaseSessionStore with full session recovery on startup; fix PhraseUniqueId storage bug (#127)
- **feat**: Add GameRecordId to GameSessionRecord for cross-referencing sessions and game records
- **feat**: Session state sync to DB after guess and clue events (not just game start)
- **test**: Add 9 new tests — 4 persistence status, 2 game detail controller, 3 session recovery (320 backend, 61 frontend)
- **fix**: Stale session cleanup now runs before active session query in LoadSessionsAsync
- **feat**: Sprint 40 — Persist GameRecord to DB at game start (InProgress status); game completion updates existing record (#121)
- **feat**: Persist game events (guesses, clues) incrementally to DB as they happen, not just at game end (#122)
- **feat**: Add Abandoned status to GameResult; session cleanup marks expired registered games as Abandoned in DB (#123)
- **feat**: Make RecordClueEvent and RemoveExpiredSessions async across interface, service, controller, and cleanup service
- **test**: Add/update GamePersistenceTests for start-persist, incremental events, abandoned tracking, rollback (311 total)
- **feat**: Sprint 39 — Make SubmitGuess and GiveUp async end-to-end; persistence errors now propagate to caller (#124, #118)
- **feat**: Wrap GameRecord + GameEvents persistence in UnitOfWork transaction for atomic saves (#119)
- **feat**: Add GetByGameIdWithEventsAsync and GetByUserIdWithEventsAsync to IGameRecordRepository (#120)
- **feat**: Add GET /api/game/detail/{gameId} endpoint for full game record with events (#120)
- **test**: Add 11 new GamePersistenceTests covering transaction, rollback, guest skip, analytics isolation, event recording (#128)
- **test**: Update ScoringTests, SessionManagementTests, GameControllerTests, GameServiceTests for async API (306 total)

### 2026-04-11
- **fix**: Sprint 38 — Implement automatic token refresh in AuthContext; admin menu now persists across JWT expiry (#116)
- **fix**: Include roles in UpdateUser API response to prevent admin status loss after profile edits (#117)
- **test**: Add UpdateUser_ReturnsRolesInResponse backend test (295 total)
- **chore**: Mark backlog items #93-#115 as Done per sprint history (sprints 28-31)

### 2026-04-10
- **docs**: Sprint 37 — growing complexity management: code-architect update, codebase map, complexity budget, code-simplifier awareness
- **docs**: Update code-architect agent with full current architecture (12 services, 9 controllers, EF Core, JWT, admin, analytics, simulation)
- **docs**: Add Codebase Map to copilot-instructions.md for quick navigation reference
- **docs**: Update code-simplifier with complexity awareness (God objects, hook bloat, inline styles, deep nesting signals)
- **docs**: Add Complexity Check to SPRINT_EXECUTION_WORKFLOW.md Phase 2 (architecture review triggers)
- **docs**: Sprint 36 — developer practice gaps: lessons-learned registry, pre-commit checklist, test expectations in plans, npm run build enforcement, retro lesson encoding
- **docs**: Create Lessons Learned registry in copilot-instructions.md (L1-L11 indexed from 13 retrospectives)
- **docs**: Add Pre-Commit Checklist to verify-app agent (unused imports, test coverage, API signatures, grep-before-delete)
- **docs**: Update build-validator agent and full-test skill to enforce `npm run build` over `tsc --noEmit`
- **docs**: Add test coverage expectations requirement to sprint plan template (Phase 1)
- **docs**: Add Retro Lesson Encoding step to Phase 5 workflow
- **docs**: Sprint 35 — context exhaustion prevention: context-efficiency guidelines in all agents, task-level sprint tracking, context budget estimation, session handoff checklist
- **docs**: Add Context Efficiency sections to build-validator, code-architect, code-simplifier, verify-app agents
- **docs**: Add Context Budget Estimation rules to SPRINT_EXECUTION_WORKFLOW.md Phase 3
- **docs**: Add Session Handoff Checklist to SPRINT_STOPPING_CRITERIA.md
- **docs**: Enhance sprint-status.json with per-task tracking (tasks array)
- **docs**: Add context efficiency and memory-save rules to copilot-instructions.md
- **docs**: Sprint 34 — fix backlog drift: mark 60+ completed items, update copilot-instructions with current architecture (MySQL/EF Core, JWT auth, roles, admin, analytics, simulation)
- **docs**: Update User model in copilot-instructions to reflect soft-delete, PasswordHash, and GameRecord decoupling (Sprint 13+)
- **docs**: Replace outdated JSON file storage docs with MySQL/EF Core data access layer documentation
- **docs**: Update Backend Patterns with current service layer, auth, EF Core patterns, and controller inventory
- **docs**: Add Backlog Hygiene checklist to SPRINT_EXECUTION_WORKFLOW.md Phase 5
- **feat**: Unified auth system — single token for player and admin, no more re-login when navigating to admin pages (#113, #114, #115)
- **feat**: Add AuthContext with useAuth hook for centralized auth state management (#114)
- **fix**: Admin users no longer forced to re-login when accessing admin pages (#113)
- **chore**: Remove redundant AdminLogin page and dual-token system (#115)
- **fix**: Apply pending AddPhraseUniqueIdToGameRecord migration — column missing from DB caused MySqlException (#112)
- **feat**: Auto-apply EF Core migrations on startup in Development environment (#112)
- **test**: Add integration test for GameRecord PhraseUniqueId query and filter (#112)
- **fix**: Persist GameEvents to database — events were ignored by EF Core (#103)
- **fix**: Recompute PlayerStats after each game completion (#104)
- **fix**: Recompute PhrasePlayStats after each game completion (#105)
- **fix**: Admin nav disappears on refresh — roles now included in UserResponse (#106)
- **fix**: Add roles field to UserResponse DTO and populate from backend (#107)
- **fix**: Admin games page shows 0 events — events now persisted explicitly (#112)
- **fix**: PhrasePlayStats query uses PhraseUniqueId instead of PhraseText (#113)
- **feat**: Add PhraseUniqueId column to GameRecord with EF migration (#113)
- **fix**: Consistent adminApi error handling — all methods use shared handleResponse (#108)
- **fix**: AdminGuard now verifies JWT contains admin role, not just token existence (#109)
- **fix**: Graceful empty PlayerStats — shows helpful message instead of blank (#110)
- **feat**: Add per-game ClueEffectiveness recomputation after game completion (#111)
- **feat**: Add reusable ConfirmDialog component for destructive admin actions (#97)
- **feat**: Add audit log API endpoint and admin viewer page with filters (#98)
- **chore**: Extract inline styles to CSS files for AdminDashboard, AdminGames, AdminConfig, AdminDataExplorer (#99)
- **feat**: Add dashboard manual refresh button and auto-refresh toggle (30s) (#100)
- **fix**: Fix 6 TypeScript build errors — GameBoard props, unused imports, test mocks, vitest config (#93)
- **feat**: Add phrase admin CRUD API endpoints at /api/admin/phrases with pagination and stats (#102)
- **feat**: Add user search parameter to admin GET /api/admin/users endpoint (#95)
- **feat**: Add role management API — GET/POST/DELETE /api/admin/users/{id}/roles (#96)
- **feat**: Add admin phrases management page with search, create/edit, activate/deactivate, and stats (#94)
- **feat**: Add user search input with clear to AdminUsers page (#95)
- **feat**: Add role badges display and role assignment/removal to AdminUsers page (#96)
- **chore**: Extract AdminUsers inline styles to AdminUsers.css (#99)
- **feat**: Add roles to auth response and JWT claims — login/register/refresh now return user roles
- **feat**: Show Admin nav link for admin users (roleId 3) in main NavHeader between Leaderboard and user section
- **fix**: Fix pre-existing GameHistoryPage test missing `loginUser` mock
- **test**: Add NavHeader admin link tests (4 new) — visibility for admin/non-admin/guest, correct href
- **test**: Add AuthService roles tests (3 new) — admin roles in response, empty roles for regular users, role claims in JWT
- **feat**: Add admin frontend — login, dashboard, user management, games manager, site config, data explorer (#86-#92)
- **feat**: Admin route guards with JWT validation and sidebar layout (#87)
- **feat**: Admin API service with separate token management for admin sessions (#86)
- **fix**: Fix SessionCleanupService captive dependency — use IServiceProvider + scope creation (#82)
- **fix**: Update DatabaseSeedService admin user to name `admin` with correct password and Admin role assignment (#84)
- **chore**: Apply InitialCreate EF Core migration to MySQL — 17 tables created (#82)
- **chore**: Run JSON-to-MySQL data migration — 3 users, 10 phrases imported (#83)
- **test**: Fix DatabaseSeedServiceTests for updated admin user name and IRoleService mock (#84)

### 2026-04-09
- **feat**: Add ASP.NET Core health check infrastructure with JSON/MySQL provider checks (#67)
- **feat**: Add DatabaseSeedService — admin user in all environments, test data in Development (#68)
- **test**: Add 7 health check and database seeding tests (195 backend total)
- **feat**: Decouple GameRecords from User model — Games property removed, separate JSON file storage (#65)
- **feat**: JsonGameRecordRepository now stores records as individual JSON files in Data/GameRecords/ (#65)
- **feat**: Add GetGameCountAsync to IUserService for leaderboard game counts (#65)
- **feat**: GamePhraseService uses IGameRecordRepository for played-phrase lookup (#65)
- **feat**: UserController leaderboard and MapToResponse no longer depend on User.Games (#65)
- **feat**: Remove AddGameRecordAsync from IUserService — persistence handled by GameService (#66)
- **feat**: Extract ISessionStore from GameService — ConcurrentDictionary-backed Singleton (#63)
- **feat**: GameService now uses ISessionStore + IGameRecordRepository for session management (#63, #45)
- **feat**: Game completion persists GameRecord via IGameRecordRepository + updates user points (#45)
- **feat**: GameEvents populated with GameId and SequenceNumber for STI storage (#46)
- **feat**: GameService Scoped-compatible when using MySql data provider (#63)
- **feat**: Add GetGameRecordAsync to IGameService for retrieving session game records (#45)
- **feat**: Add EF Core infrastructure with MySQL support via Pomelo provider (#25)
- **feat**: Add LinkittyDoDbContext with Fluent API entity configurations for Users, GamePhrases, GameRecords, GameEvents (#25)
- **feat**: Extract IGameRecordRepository — GameRecords as separate aggregate from User.Games (#26)
- **feat**: Add IUnitOfWork interface for transactional consistency across repositories (#26)
- **feat**: Add EfUserRepository, EfGamePhraseRepository, EfGameRecordRepository implementations (#25)
- **feat**: Add DataProvider feature flag — "Json" or "MySql" toggle in appsettings (#64)
- **feat**: Add conditional DI registration — switches between Json/Scoped EF repositories (#64)
- **feat**: Add soft-delete support (IsActive, DeletedAt) on User and GamePhrase models (#25)
- **test**: Add 15 EF Core infrastructure tests — schema, repositories, STI events (188 backend total)
- **feat**: Add JWT authentication — register, login, and refresh token endpoints (#22)
- **feat**: Add BCrypt password hashing for secure credential storage (#22)
- **feat**: Protect user-specific endpoints (update, delete, difficulty, points, games) with [Authorize] (#22)
- **feat**: Add frontend login/register with password, token storage in localStorage (#22)
- **feat**: Add auth tab UI (Sign In / Register) replacing old datalist user picker (#22)
- **test**: Add 10 AuthService tests — register, login, refresh, revoke, JWT validation (173 backend total)
- **feat**: Remove dead HomePage component and hero section — game starts immediately on /play (#37)
- **feat**: Scale phrase database from 26 to 110 phrases across difficulty bands (#7)
- **feat**: Add sound effects for correct/incorrect guesses, game solve, and give up using Web Audio API (#20)
- **feat**: Add mute toggle button in game footer with localStorage persistence (#20)
- **feat**: Sound effects respect prefers-reduced-motion media query (#20)
- **test**: Add 6 phrase database tests — count verification, duplicate detection, format validation (163 backend total)
- **test**: Add 7 GameBoard layout tests — structure, footer content, loading state, mute toggle (57 frontend total)
- **feat**: Add input sanitization with DataAnnotation validation on GuessRequest and StartGameRequest (#23)
- **feat**: Add rate limiting middleware — game-start 10/min, clue 30/min, user 60/min, global 100/min per IP (#24)
- **test**: Add 12 input validation tests covering XSS prevention, boundary checks, and format validation (157 backend total)
- **feat**: Add leaderboard page with backend API GET /api/user/leaderboard and ranked table UI (#18)
- **feat**: Add game timer displaying elapsed time during gameplay (#19)
- **feat**: Add consecutive correct guess streak indicator with visual animation (#19)
- **feat**: Add keyboard shortcuts — G for give up, N for new game after completion (#21)
- **feat**: Add ARIA labels for word slots, game area, leaderboard table, and victory announcements (#21)
- **feat**: Add prefers-reduced-motion support to disable all animations globally (#21)
- **feat**: Add keyboard shortcut hints in game footer (#21)
- **test**: Add 6 backend leaderboard tests (136 backend total)
- **test**: Add 6 frontend leaderboard tests (55 frontend total)
- **feat**: Add xnym taxonomy expansion — antonyms (rel_ant) for difficulty>60, homophones (rel_hom) for difficulty>80 (#12)
- **feat**: Add contextual synonym disambiguation — left/right context (lc=/rc=) params for Datamuse ml queries (#13)
- **feat**: Add in-memory clue caching with 7-day TTL — ConcurrentDictionary cache for synonym lookups (#14)
- **test**: Add 10 new backend tests for xnym expansion, contextual synonyms, and clue caching (130 backend total)
- **feat**: Add React Router with page structure - routes for home (/), play (/play), and history (/history) (#8)
- **feat**: Add navigation header with active route highlighting, user display, and guest detection
- **feat**: Create home page with hero section, how-to-play guide, and user stats for registered users (#9)
- **feat**: Add game history page with expandable event timeline, game results, and score display (#10)
- **feat**: Add GitHub Actions CI/CD pipeline for backend build/test and frontend lint/test/build (#15)
- **feat**: Add session TTL with configurable expiry (default 24h) and background cleanup service (#16)
- **feat**: Enhance health endpoint with uptime, active session count, and data directory status (#17)
- **feat**: Add responsive mobile layout for NavHeader on small screens (#11)
- **test**: Add 8 new backend tests for session management (120 backend total)
- **test**: Add 20 new frontend tests for NavHeader, HomePage, and GameHistoryPage (49 frontend total)

### 2026-04-05
- **feat**: Implement difficulty-aware clue selection - synonym ranking and URL preference vary by difficulty tier (#4)
- **feat**: Implement enhanced scoring formula - BasePoints/(clues×guesses) with difficulty scaling and first-guess bonus (#5)
- **feat**: Add phrase difficulty computation - auto-computed from word count, hidden ratio, and word length (#6)
- **feat**: Phrase selection prefers phrases near user's preferred difficulty (±20 range)
- **test**: Add 42 new tests for scoring formula, difficulty tiers, clue selection, and phrase difficulty (112 total)
- **feat**: Add `start-app.bat` and `stop-app.bat` for one-click app startup (#36)
- **docs**: Add quick-start instructions to README
- **feat**: Standardize all API responses with `ApiResponse<T>` wrapper and structured error responses (#3)
- **test**: Add backend test suite with xUnit and Moq - 70 tests covering controllers and services (#1)
- **test**: Add frontend test suite with Vitest and Testing Library - 29 tests covering API service and components (#2)
- **chore**: Establish sprint-based agile framework with agents, skills, and workflow documentation
