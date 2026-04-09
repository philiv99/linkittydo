# Sprint 13 Plan — User Model Decoupling + Migration Tool

**Sprint Goal**: Decouple GameRecords from User model, delegate game history to IGameRecordRepository, create JSON-to-DB migration tool, and add MySQL health check.

**Backlog Items**: #65 (User model refactoring), #66 (JSON→MySQL migration tool), #67 (MySQL health check)

**Branch**: `feature/20260409-sprint-13`

## Tasks

1. Remove `List<GameRecord> Games` from User model
2. Remove AddGameRecordAsync from IUserService/UserService
3. Update UserService.GetUserGamesAsync to use IGameRecordRepository
4. Update GameController — remove duplicate AddGameRecordAsync calls
5. Update UserController game history endpoint to use IGameRecordRepository
6. Create DataMigrationService for JSON→DB migration
7. Add MySQL health check to /health endpoint
8. Fix all tests, add new tests
9. Update CHANGELOG.md

## Acceptance Criteria

- [ ] User model no longer contains Games property
- [ ] Game history retrieved via IGameRecordRepository
- [ ] GameController no longer duplicates persistence
- [ ] Migration tool can read JSON users and insert normalized records  
- [ ] Health check reports MySQL connectivity
- [ ] All tests pass
