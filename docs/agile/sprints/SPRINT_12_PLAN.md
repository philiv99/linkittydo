# Sprint 12 Plan — DI Lifetime Migration + Game Data Integration

**Sprint Goal**: Extract ISessionStore from GameService, migrate DI lifetimes to support Scoped repositories, and wire game completion to IGameRecordRepository.

**Backlog Items**: #63 (DI lifetime migration), #45 (GameRecords usage), #46 (GameEvents STI usage)

**Branch**: `feature/20260409-sprint-12`

## Tasks

1. Create ISessionStore interface + InMemorySessionStore (Singleton) to hold sessions
2. Refactor GameService to use ISessionStore instead of internal dictionary
3. Make GameService Scoped-compatible (all state in ISessionStore)
4. Wire GameService game completion to IGameRecordRepository (persist completed games)
5. Update GameController to use IGameRecordRepository for game record endpoint
6. Add integration tests
7. Update CHANGELOG.md

## Acceptance Criteria

- [ ] ISessionStore extracted as Singleton service
- [ ] GameService is Scoped when using MySql provider
- [ ] Completed games are persisted via IGameRecordRepository
- [ ] GameEvents include proper SequenceNumber and GameId
- [ ] All existing tests pass + new tests pass
