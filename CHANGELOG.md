# Changelog

All notable changes to LinkittyDo will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### 2026-04-09
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
