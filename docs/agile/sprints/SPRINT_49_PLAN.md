# Sprint 49 Plan

**Goal**: Remove legacy JSON data files and JSON repository code now that MySQL is the sole data provider
**Duration**: 2026-04-12
**Estimated effort**: 4 hours

## Selected Items

### 1. Remove legacy JSON data files and JSON repository code (Backlog #140)

- **Source**: Backlog item #140
- **Data Rule**: Only Admin and TestPlayer exist in the database. The 3 JSON user files (Frank, Tom, Sally) are stale test data and must NOT be migrated. They will be deleted with the rest of the JSON files.

- **Tasks**:
  - A. Verify MySQL database contains Admin and TestPlayer users and all phrase data; confirm no stale JSON users (Frank, Tom, Sally) are in the DB (~0.5h)
  - B. Remove JSON data files: `Data/Phrases/`, `Data/Users/`, `Data/GameRecords/` (~0.5h)
  - C. Remove JSON repository classes: `JsonUserRepository.cs`, `JsonGamePhraseRepository.cs`, `JsonGameRecordRepository.cs` (~0.5h)
  - D. Remove `DataProvider` feature flag and JSON branch from `Program.cs` — MySQL becomes the only code path (~0.5h)
  - E. Remove `JsonToMySqlMigrationService`, `IDataMigrationService`, `MigrationController` (~0.5h)
  - F. Remove `JsonStorageHealthCheck` from `HealthChecks.cs` and the `.csproj` JSON copy rule (~0.25h)
  - G. Remove or update `PhraseDatabaseTests.cs` (depends on JSON repo) and any other affected tests (~0.5h)
  - H. Remove `NoOpRoleService`, `NoOpAuditService`, `NoOpAnalyticsService`, `InMemorySiteConfigService` JSON-only services (~0.25h)
  - I. Build and test validation (backend + frontend) (~0.5h)

- **Acceptance Criteria**:
  - [ ] No JSON data files remain in `src/LinkittyDo.Api/Data/`
  - [ ] No JSON repository classes remain in the codebase
  - [ ] `DataProvider` setting and conditional branching removed from `Program.cs`
  - [ ] Migration service and controller removed
  - [ ] `.csproj` no longer copies `Data\**\*.json`
  - [ ] `dotnet build` succeeds with zero errors
  - [ ] `dotnet test` passes (all existing tests pass or are properly updated)
  - [ ] Frontend `npm run build` still succeeds

- **Risk**: Low — MySQL has been the active provider since Sprint 8. JSON code is dead code.

## Risks & Mitigations
| Risk | Impact | Mitigation |
|------|--------|------------|
| Test code depends on JSON repos | Low | Update or remove `PhraseDatabaseTests.cs` |
| Some service only registered in JSON branch | Low | Grep for all services in the JSON else-block before removing |
