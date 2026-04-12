# LinkittyDo Backlog

Master backlog of improvement ideas for LinkittyDo. Items are prioritized and grouped by category.

**Last Updated**: 2026-04-12 (Full audit — all 140+ completed items removed, fresh ideas added)

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

## Current State Summary

As of Sprint 50 (April 2026), LinkittyDo is a fully functional word-guessing game with:
- ASP.NET Core 8 backend with MySQL/EF Core, JWT auth, role-based authorization
- React/TypeScript frontend with routing, responsive layout, accessibility
- 115 curated phrases with difficulty scaling
- Full admin panel (dashboard, users, games, phrases, config, data explorer, audit log)
- Simulation engine for generating test gameplay data
- Analytics (PlayerStats, PhrasePlayStats, ClueEffectiveness)
- CI/CD via GitHub Actions, 393 tests (329 backend, 64 frontend)

---

## Backlog Items

### Gameplay & Player Experience

| # | Item | Priority | Notes |
|---|------|----------|-------|
| ~~1~~ | ~~Daily challenge mode~~ | ~~P2~~ | ~~Completed Sprint 51~~ |
| ~~2~~ | ~~Tutorial / onboarding flow~~ | ~~P2~~ | ~~Completed Sprint 51~~ |
| ~~3~~ | ~~Player profile page~~ | ~~P2~~ | ~~Completed Sprint 51~~ |
| 4 | Contextual synonym selection | P3 | Use Datamuse `lc=`/`rc=` parameters to disambiguate polysemous words (e.g., "bank" in a financial vs. river context). Improves clue relevance, especially at higher difficulty. |
| 5 | Clue pre-computation and caching | P3 | Pre-compute synonym → URL mappings for all active phrases. Cache with a configurable TTL (e.g., 7 days). Validate cached URLs in a background job. Reduces clue fetch latency and improves reliability. |
| 6 | Hint progression system | P3 | Instead of one clue type, offer a tiered hint system: (1) just the URL domain, (2) full URL, (3) URL + relationship type label. Each tier costs more points. Gives players more agency over risk/reward. |
| 7 | Game replay viewer | P4 | Let users (and admins) replay a completed game step-by-step: see each clue revealed, each guess submitted, in order. Uses existing GameEvents data. Useful for learning, debugging, and sharing. |

### Visual Design & Polish

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 8 | Dark mode toggle | P2 | CSS already has `prefers-color-scheme` detection. Add an explicit toggle in NavHeader that saves preference to localStorage. Define dark/light theme variables for all components. |
| 9 | Animations and transitions | P3 | Add subtle CSS animations: word reveal (flip/fade), correct guess (glow/pulse), wrong guess (shake), score increment (count-up), clue panel tab switch (slide). Respect `prefers-reduced-motion`. |
| 10 | Visual feedback for streaks | P3 | When a player has an active streak (consecutive correct guesses), show a visual indicator — flame icon, streak counter badge, or color change on the score display. Motivates continued play. |
| 11 | Loading states and skeleton screens | P3 | Replace raw spinners with skeleton loading patterns for: game board initialization, leaderboard fetch, game history page, admin tables. Improves perceived performance. |

### Production Readiness & Deployment

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 12 | Docker Compose for full-stack local dev | P2 | Single `docker-compose up` to start MySQL, backend API, and frontend dev server. Eliminates manual setup of MySQL and multi-terminal startup. Include a `.env.example` for configuration. |
| 13 | Production deployment configuration | P2 | Document and configure a production deployment path: environment-specific `appsettings.Production.json`, HTTPS enforcement, CORS lockdown to real domain, connection string from environment variables (not config file). Consider Docker image publishing. |
| 14 | Error monitoring integration | P3 | Add structured error logging and consider integration with an error tracking service (e.g., Sentry, Application Insights). Backend: global exception handler with correlation IDs. Frontend: error boundary with reporting. |
| 15 | Database backup and restore tooling | P3 | Scripts or documentation for MySQL backup/restore. Important before any production deployment. Could be a simple `mysqldump` wrapper or an admin API endpoint for on-demand backup. |
| 16 | Environment-based configuration | P3 | Frontend currently hardcodes `localhost:5157` as the API URL. Move to environment-based configuration (`VITE_API_URL`) with `.env.development` and `.env.production` files. |

### Admin Enhancements

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 17 | Admin data export (CSV) | P3 | Add CSV export buttons to Admin Users, Admin Games, and Admin Phrases pages. Backend endpoints return `text/csv` with appropriate headers. Useful for offline analysis and reporting. |
| 18 | Phrase batch import | P3 | Admin ability to bulk-import phrases from a CSV or JSON file. Validate format, check for duplicates, compute difficulty, and preview before committing. Faster than one-by-one creation. |
| 19 | Admin dashboard charts | P3 | Replace plain stat cards with simple charts: games per day (line chart), difficulty distribution (histogram), solve rate trend (line chart). Use a lightweight charting library (e.g., Recharts). |
| 20 | Phrase category management UI | P4 | PhraseCategories table exists but there is no admin UI to create, edit, or assign categories. Add a category management section to the Admin Phrases page. |

### Advanced Linguistic Features

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 21 | POS tagging for smarter word hiding | P3 | Replace the stop-word list with a real part-of-speech tagger for phrase decomposition. Ensures content words (nouns, verbs, adjectives) are hidden while function words (articles, prepositions) stay visible. Consider a lightweight NLP API or library. |
| 22 | Adaptive difficulty auto-tuning | P3 | Use PhrasePlayStats (solve rate, avg clues to solve) to automatically recalibrate phrase difficulty. Phrases that most players solve easily should decrease in difficulty; phrases with low solve rates should increase. Runs as a background job after every N games. |
| 23 | Reinforcement learning for clue selection (LinUCB) | P4 | Use ClueEffectiveness data to train a contextual bandit that selects the best synonym and URL type for each word. Feature vector includes word frequency, phrase difficulty, player skill level. Long-term goal from the original vision document. |

### Social & Community Features

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 24 | Share game results | P3 | After completing a game, offer a "Share" button that copies a spoiler-free results summary to clipboard (Wordle-style grid). Include score, clue count, and time. No database changes needed. |
| 25 | Community phrase submission | P4 | Registered users can submit phrase suggestions via `POST /api/phrases/submit`. Submissions enter a review queue (PhraseReview table already exists). Rate-limited to prevent spam. Moderators approve/reject via Admin Phrases page. |
| 26 | Societies / groups | P4 | Social groups with shared leaderboards and activity feeds. Design docs and ER diagrams exist in `docs/sources/`. Includes: create/join groups, group leaderboard (filtered by members), group activity feed. Large feature — multiple sprints. |
| 27 | Player achievements and badges | P4 | Award badges for milestones: First Win, 10-Game Streak, 1000 Lifetime Points, Speed Demon (solved under 60s), No Wrong Guesses. Achievements table and UserAchievements junction. Show earned badges on profile page. |

### Bugs

| # | Item | Priority | Notes |
|---|------|----------|-------|
| ~~B1~~ | ~~Profile nav redirects to Play (starts new game)~~ | ~~P1~~ | ~~Completed Sprint 52~~ |
| ~~B2~~ | ~~History page shows no games for logged-in user~~ | ~~P1~~ | ~~Completed Sprint 52~~ |
| ~~B3~~ | ~~Header points not up-to-date~~ | ~~P1~~ | ~~Completed Sprint 52~~ |
| ~~B4~~ | ~~ProfilePage.test.tsx build error (GameResult type)~~ | ~~P1~~ | ~~Completed Sprint 52~~ |

### Technical Debt & Quality

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 28 | Increase frontend test coverage | P2 | 64 frontend tests cover basics, but hooks (useGame, useUser, useAuth) and admin pages need deeper coverage. Target: test all user-facing flows (register, login, play game, view history, view leaderboard). |
| 29 | API integration tests | P3 | Add end-to-end integration tests that hit the real API (in-memory test server via `WebApplicationFactory`). Cover critical paths: register → login → start game → get clue → guess → complete → verify game history. |
| 30 | Performance profiling and optimization | P3 | Profile API response times for key endpoints (game start, clue fetch, leaderboard). Identify slow queries. Add appropriate database indexes if missing. Consider response caching for leaderboard and phrase stats. |
| 31 | Dependency audit and update | P4 | Review all NuGet and npm dependencies for security vulnerabilities and outdated versions. Update to latest stable versions. Run `dotnet list package --outdated` and `npm audit`. |

### Progressive Web App (PWA)

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 32 | PWA manifest and service worker | P4 | Add a web app manifest and basic service worker for installability. Users can "Add to Home Screen" on mobile. Enables offline access to static assets and cached game state. |

---

## Ideas Parking Lot

_Speculative ideas that are not yet ready for prioritization. Move to Backlog Items when refined._

- **Multiplayer mode**: Real-time head-to-head or cooperative play using WebSockets (SignalR)
- **Localization (i18n)**: Multi-language support for UI and phrases (requires phrase sets per language)
- **Mobile native app**: React Native or .NET MAUI wrapper for App Store/Play Store presence
- **LLM-powered clue generation**: Use OpenAI to generate creative, difficulty-appropriate clues as an alternative to URL-based clues
- **Phrase difficulty preview**: Before starting a game, show the player the phrase difficulty and let them skip/accept
- **Timed challenge mode**: Countdown timer with escalating hints — pressure-based gameplay variant
- **Public API for phrase contributions**: Open API for third-party phrase packs (themed: movies, science, history)
- **Accessibility audit by specialist**: Professional accessibility review beyond the current ARIA/keyboard implementation
