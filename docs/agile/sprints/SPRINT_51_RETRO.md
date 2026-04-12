# Sprint 51 Retrospective

**Date**: 2026-04-12
**Sprint Goal**: Implement first 3 Gameplay & Player Experience backlog items (Daily Challenge Mode, Tutorial/Onboarding, Player Profile Page)
**Result**: Complete — all 3 features delivered with tests

## Delivered

| # | Item | Status |
|---|------|--------|
| 1 | Daily Challenge Mode | Done |
| 2 | Tutorial / Onboarding Flow | Done |
| 3 | Player Profile Page | Done |

## Metrics

| Metric | Before | After | Delta |
|--------|--------|-------|-------|
| Backend tests | 329 | 339 | +10 |
| Frontend tests | 64 | 89 | +25 |
| Backend build | Pass | Pass | — |
| Frontend build | Pass | Pass | — |
| Lint errors | 0 | 0 | — |

## What Went Well

- **Batch feature delivery**: Three features delivered in a single sprint with full test coverage
- **Test-driven fixes**: Running tests early caught constructor signature mismatches across 6 test files
- **Incremental validation**: Building backend first, then frontend, then tests allowed quick iteration

## What Could Be Improved

- **Constructor dependency impact**: Adding `IDailyChallengeService` to `GameService` and `IAnalyticsService` to `UserController` broke 6 existing test files. Grepping test files for constructor calls before modifying constructors would have prevented surprise failures.
- **Frontend scaffolding cleanup**: `DailyChallengePage` was initially scaffolded with unused game-playing components (`useGame`, `CluePanel`, `PhraseDisplay`) that needed manual cleanup. Starting with a leaner template would save time.

## Lessons Learned

- **L21**: When adding a new dependency to a service/controller constructor, immediately grep all test files that instantiate that class and update them in the same commit. This prevents cascading build failures.
- **L22**: When scaffolding a new page component, start minimal (just the API call and basic display) rather than including game-play components that may not apply to the page's final design.

## Files Changed

### New Files (14)
- `src/LinkittyDo.Api/Models/DailyChallenge.cs`
- `src/LinkittyDo.Api/Models/ProfileResponse.cs`
- `src/LinkittyDo.Api/Services/IDailyChallengeService.cs`
- `src/LinkittyDo.Api/Services/DailyChallengeService.cs`
- `src/LinkittyDo.Api/Controllers/DailyChallengeController.cs`
- `src/LinkittyDo.Api.Tests/DailyChallengeControllerTests.cs`
- `src/linkittydo-web/src/pages/TutorialPage.tsx`
- `src/linkittydo-web/src/pages/TutorialPage.css`
- `src/linkittydo-web/src/pages/ProfilePage.tsx`
- `src/linkittydo-web/src/pages/ProfilePage.css`
- `src/linkittydo-web/src/pages/DailyChallengePage.tsx`
- `src/linkittydo-web/src/pages/DailyChallengePage.css`
- `src/linkittydo-web/src/test/TutorialPage.test.tsx`
- `src/linkittydo-web/src/test/ProfilePage.test.tsx`
- `src/linkittydo-web/src/test/DailyChallengePage.test.tsx`

### Modified Files (16)
- `src/LinkittyDo.Api/Models/GameSession.cs` (added IsDailyChallenge flag)
- `src/LinkittyDo.Api/Services/GameService.cs` (added StartDailyChallengeAsync, daily challenge result recording)
- `src/LinkittyDo.Api/Services/IGamePhraseService.cs` (added GetPhraseByUniqueIdAsync)
- `src/LinkittyDo.Api/Services/GamePhraseService.cs` (implemented GetPhraseByUniqueIdAsync)
- `src/LinkittyDo.Api/Controllers/UserController.cs` (added profile endpoint, IAnalyticsService dependency)
- `src/LinkittyDo.Api/Data/LinkittyDoDbContext.cs` (added DailyChallenge/DailyChallengeResult DbSets)
- `src/LinkittyDo.Api/Program.cs` (registered IDailyChallengeService)
- `src/linkittydo-web/src/types/index.ts` (added Daily/Profile types)
- `src/linkittydo-web/src/services/api.ts` (added Daily/Profile API methods)
- `src/linkittydo-web/src/App.tsx` (added routes)
- `src/linkittydo-web/src/components/NavHeader.tsx` (added nav links)
- `src/linkittydo-web/src/test/NavHeader.test.tsx` (added 4 tests for new links)
- `src/LinkittyDo.Api.Tests/UserControllerTests.cs` (added IAnalyticsService mock, 3 profile tests)
- `src/LinkittyDo.Api.Tests/GameServiceTests.cs` (added IDailyChallengeService mock)
- `src/LinkittyDo.Api.Tests/GamePersistenceTests.cs` (added IDailyChallengeService mock)
- `src/LinkittyDo.Api.Tests/SessionManagementTests.cs` (added IDailyChallengeService mock)
- `src/LinkittyDo.Api.Tests/ScoringTests.cs` (added IDailyChallengeService mock)
- `src/LinkittyDo.Api.Tests/LeaderboardTests.cs` (added IAnalyticsService mock)
- `README.md` (added features)
- `CHANGELOG.md` (added sprint 51 entries)
- `docs/agile/BACKLOG.md` (marked items 1-3 complete)
- `docs/agile/sprint-status.json` (updated to sprint 51)
