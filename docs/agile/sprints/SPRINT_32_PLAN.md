# Sprint 32 Plan: Bug Fix — PhraseUniqueId Column Missing from Database

**Sprint Goal**: Fix the `Unknown column 'g.PhraseUniqueId'` MySQL error by applying the pending migration, adding automatic migration on startup, and auditing all EF Core model-to-database column mappings for consistency.

**Date**: 2026-04-10
**Branch**: `feature/20260410-sprint-32`
**Backlog Items**: #112
**Type**: Bug Fix

---

## Root Cause Analysis

The error:
```
MySqlConnector.MySqlException (0x80004005): Unknown column 'g.PhraseUniqueId' in 'field list'
```

**Cause**: Migration `20260410142036_AddPhraseUniqueIdToGameRecord` was generated (adds `PhraseUniqueId VARCHAR(30)` to `GameRecords` table) but never applied to the running MySQL database. The EF Core model includes the property, so any LINQ query against `GameRecords` that touches `PhraseUniqueId` generates SQL referencing a column that doesn't exist.

**Affected queries**: `AnalyticsService.RecomputePhrasePlayStatsAsync()` — `WHERE g.PhraseUniqueId == phraseUniqueId`

---

## Items

### Item 1: Apply Pending Migration & Add Startup Migration (#112)
**Priority**: P1 — Blocks analytics functionality
**Acceptance Criteria**:
- [ ] The `AddPhraseUniqueIdToGameRecord` migration is applied to the database
- [ ] `Program.cs` applies pending EF Core migrations automatically on startup (Development environment only)
- [ ] The `PhraseUniqueId` column exists in `GameRecords` table after startup
- [ ] `AnalyticsService.RecomputePhrasePlayStatsAsync()` no longer throws

**Tasks**:
1. Add `Database.MigrateAsync()` call in `Program.cs` (scoped to Development environment; production should use explicit migration commands)
2. Verify the migration applies cleanly against the current database state
3. Test `RecomputePhrasePlayStatsAsync` with a game record that has a `PhraseUniqueId`

### Item 2: Audit EF Core Model-to-Database Column Consistency
**Priority**: P2 — Prevent similar issues
**Acceptance Criteria**:
- [ ] All properties in EF Core models that map to database columns are verified against migration history
- [ ] Any other missing columns or mismatches are identified and documented
- [ ] `GameRecord.IsSimulated` column is confirmed present (added in InitialCreate — OK)
- [ ] No orphaned model properties exist without corresponding migration

**Tasks**:
1. Compare `GameRecord` model properties against `InitialCreate` + `AddPhraseUniqueIdToGameRecord` DDL
2. Compare all other models (`GamePhrase`, `User`, `GameEvent`, `PlayerStats`, `PhrasePlayStats`, `ClueEffectiveness`, `SimulationProfile`, `PhraseReview`, `PhraseCategoryAssignment`) against their migrations
3. List any discrepancies found

### Item 3: Add Integration Test for GameRecord PhraseUniqueId
**Priority**: P2 — Regression prevention
**Acceptance Criteria**:
- [ ] Test creates a `GameRecord` with `PhraseUniqueId` set
- [ ] Test queries `GameRecords` filtering by `PhraseUniqueId`
- [ ] Test verifies `RecomputePhrasePlayStatsAsync` works end-to-end with EF Core in-memory or SQLite provider

**Tasks**:
1. Add test in `AnalyticsServiceTests.cs` (or new test file) covering the `PhraseUniqueId` query path
2. Ensure test uses `EnsureCreated()` which builds from the current model snapshot (verifying all columns exist)
3. Run full test suite to confirm no regressions

---

## Definition of Done

- [ ] `dotnet build` passes with 0 errors
- [ ] `dotnet test` passes with all existing + new tests green
- [ ] App starts without migration errors
- [ ] Analytics queries that reference `PhraseUniqueId` work correctly
- [ ] No other model-to-DB mismatches identified (or fixes applied)
- [ ] CHANGELOG.md updated
