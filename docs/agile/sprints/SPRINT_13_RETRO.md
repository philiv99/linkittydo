# Sprint 13 Retrospective

## Sprint Goal
Decouple GameRecords from User model — User becomes a pure profile aggregate, GameRecords live independently.

## Delivered
- Removed `List<GameRecord> Games` from User model
- Rewrote JsonGameRecordRepository to use standalone JSON files (Data/GameRecords/{gameId}.json)
- Removed AddGameRecordAsync from IUserService (GameService handles persistence)
- Updated GamePhraseService to use IGameRecordRepository for played-phrase lookup
- Updated UserController (leaderboard, MapToResponse) to use GetGameCountAsync
- Updated AuthService to not set Games on new user creation
- Fixed all test files (UserServiceTests, GameControllerTests, LeaderboardTests, UserControllerTests)
- All 188 backend tests pass, 57 frontend tests pass

## Backlog Items
- #65: User model refactoring (decouple Games) — DONE
- #66: JSON→MySQL migration tool — DEFERRED (not critical for current sprint goal)
- #67: MySQL health check — DEFERRED (not critical for current sprint goal)

## What Went Well
- Clean separation of concerns — User is now a pure profile model
- JsonGameRecordRepository follows same file-per-entity pattern as JsonUserRepository
- No test count regression — all 188 tests pass

## What Could Be Improved
- `user.Games` references were spread across more files than initially tracked (AuthService, GamePhraseService, LeaderboardTests, UserControllerTests)
- Should do a full grep for property references before starting removal

## Lessons Learned
- When removing a property from a domain model, grep ALL source files (including test helpers) before starting — not just the files you expect to change
- JSON file storage pattern (one file per entity) is clean and consistent — good foundation for the migration tool later
