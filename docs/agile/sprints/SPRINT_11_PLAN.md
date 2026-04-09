# Sprint 11 Plan — EF Core Infrastructure & Repository Refactoring

**Sprint Goal**: Establish EF Core infrastructure with MySQL support, extract game record concerns into a separate repository, and add feature flag for data provider switching.

**Backlog Items**: #25 (EF Core + MySQL infrastructure), #26 (Repository interfaces refactoring), #64 (Data provider feature flag)

**Branch**: `feature/20260409-sprint-11`

## Tasks

1. Add NuGet packages: Pomelo.EntityFrameworkCore.MySql, Microsoft.EntityFrameworkCore.Sqlite (dev/test), EF Core Design tools
2. Create `LinkittyDoDbContext` with Fluent API entity configurations for Users and GamePhrases
3. Extract `IGameRecordRepository` interface (separate from User.Games)
4. Add `IUnitOfWork` interface wrapping SaveChangesAsync + BeginTransactionAsync
5. Add `DataProvider` feature flag to appsettings.json ("Json" | "MySql")
6. Conditional DI registration selecting Json or EF repositories based on flag
7. Add unit tests for new infrastructure
8. Update CHANGELOG.md

## Acceptance Criteria

- [ ] EF Core DbContext compiles and can create database schema
- [ ] IGameRecordRepository extracted with CRUD operations
- [ ] IUnitOfWork interface defined
- [ ] Feature flag switches between Json and EF providers
- [ ] All existing tests pass (173 backend + 57 frontend)
- [ ] New infrastructure tests pass
