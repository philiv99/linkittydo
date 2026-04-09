# Sprint 12 Retrospective — DI Lifetime Migration + Game Data Integration

## Delivered
- ISessionStore interface + InMemorySessionStore (ConcurrentDictionary, Singleton)
- GameService refactored: uses ISessionStore, IGameRecordRepository, IUserService
- Game completion (solve/give-up) persists GameRecord via IGameRecordRepository
- GameEvents include GameId and SequenceNumber fields for normalized storage
- GameService Scoped-compatible when using MySql DataProvider
- All 188 backend + 57 frontend tests pass

## What Went Well
- Clean separation: ISessionStore handles state, GameService handles logic
- Existing tests only needed constructor updates — no logic changes needed
- ConcurrentDictionary gives thread safety without explicit locking

## What Could Improve
- GameService.PersistGameRecordAsync uses fire-and-forget (`_ = PersistGameRecordAsync(session)`) which could lose records on crash — should use a reliable outbox pattern for production
- No new dedicated tests for persistence flow — covered by existing integration but could be more explicit

## Key Lesson
When migrating Singleton services with internal state to Scoped lifetime, extract the stateful component (session dictionary) into a Singleton `ISessionStore` first. This preserves state across request scopes while allowing the service logic to be Scoped.
