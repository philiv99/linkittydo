# Sprint 11 Retrospective — EF Core Infrastructure & Repository Refactoring

## Delivered
- EF Core infrastructure with LinkittyDoDbContext (Fluent API configurations)
- Pomelo MySQL + SQLite providers added
- IGameRecordRepository extracted as separate aggregate
- IUnitOfWork interface with EfUnitOfWork implementation 
- EfUserRepository, EfGamePhraseRepository, EfGameRecordRepository
- JsonGameRecordRepository (adapter wrapping User.Games for backward compatibility)
- DataProvider feature flag ("Json" | "MySql") with conditional DI registration
- Soft-delete support (IsActive, DeletedAt) on User and GamePhrase
- 15 new EF Core infrastructure tests (188 backend total)

## What Went Well
- Feature flag design allows zero-risk migration — existing JSON provider unchanged
- Soft-delete pattern prevents data loss during transition
- InMemory EF Core provider works well for fast unit tests

## What Could Improve
- EF Core InMemory provider does not enforce unique indexes — real constraint testing requires SQLite or actual MySQL
- Abstract `EventType` property on GameEvent caused EF Core discriminator mapping failure — shadow discriminator column was needed
- Test project targets net10.0 while API targets net8.0 — SQLite native bindings incompatible across runtimes

## Key Lesson
When using EF Core discriminators with polymorphic models that have abstract computed properties, use shadow properties (`HasDiscriminator<string>("Discriminator")`) instead of mapping the abstract property directly.
