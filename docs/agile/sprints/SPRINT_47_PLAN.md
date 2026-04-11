# Sprint 47 Plan — Games Manager Detail Enhancement

**Goal**: Enhance the admin Games Manager to show player names, rich event details, and relationship types for clue events.
**Date**: 2026-04-11
**Estimated effort**: ~6 hours

## Selected Items

### 1. Add RelationshipType to ClueEvent (Backlog #134)
- **Tasks**:
  - A. Add `RelationshipType` string property to `ClueEvent` model (~15min)
  - B. Create EF Core migration for new column (~15min)
  - C. Update `ClueService` to track which Datamuse endpoint produced the selected synonym and return it (~30min)
  - D. Update `GameService.RecordClueEvent` to accept and store relationship type (~15min)
  - E. Update `SimulationService` to include relationship type in simulated clue events (~10min)
- **Acceptance Criteria**:
  - [ ] ClueEvent has `RelationshipType` property (values: synonym, similar, trigger, antonym, homophone)
  - [ ] EF Core migration adds `RelationshipType` column to GameEvents table
  - [ ] ClueService returns relationship type alongside search term
  - [ ] New clue events store the relationship type
- **Risk**: Low — additive column, nullable for existing data

### 2. Games Manager: show player name, hide Game ID (Backlog #132)
- **Tasks**:
  - A. Update `GamesManagerService.SearchGamesAsync` to join with Users table for player names (~20min)
  - B. Update `GamesManagerController.SearchGames` response to include `playerName` and remove Game ID as primary column (~15min)
  - C. Update frontend `AdminGame` type to include `playerName` (~5min)
  - D. Update `AdminGames.tsx` table to show Player column instead of Game ID (~15min)
- **Acceptance Criteria**:
  - [ ] Game list shows player name instead of Game ID
  - [ ] SIM badge still shown for simulated games
  - [ ] Player name defaults to "Unknown" if user record is missing

### 3. Games Manager: rich event detail view (Backlog #133)
- **Tasks**:
  - A. Update `GamesManagerController.GetGameDetail` to return full polymorphic event data (~20min)
  - B. Update frontend `GameDetail`/`GameEventSummary` types with rich event fields (~15min)
  - C. Rewrite detail event table in `AdminGames.tsx` with rich rendering (~45min)
  - D. Add CSS for clue links, guess result badges, relationship badges (~15min)
- **Acceptance Criteria**:
  - [ ] Clue events show: clickable URL link, phrase word at WordIndex, search term with relationship type label
  - [ ] Guess events show: phrase word, guess text, correct/incorrect badge, points awarded
  - [ ] Game end events show: reason (solved/gave up)
  - [ ] PhraseText is split into words array for word-index lookups

### 4. Date+time formatting (Backlog #135)
- **Tasks**:
  - A. Update Played column to show date and time (~5min)
  - B. Update event timestamps to show date and time (~5min)
- **Acceptance Criteria**:
  - [ ] Played column shows date + time (not date only)
  - [ ] Event Time column shows date + time (not time only)

### 5. Tests
- **Tasks**:
  - A. Update `GamesManagerTests` to verify player name in search results (~20min)
  - B. Add test for full event detail including ClueEvent fields (~15min)
  - C. Verify backend build passes (~5min)
  - D. Verify frontend build passes (~5min)
- **Acceptance Criteria**:
  - [ ] All existing tests pass
  - [ ] New tests verify player name inclusion and rich event data

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| EF Core migration on existing data | Low | RelationshipType is nullable; existing rows get NULL |
| ClueService complexity increase | Low | Minimal changes — just track which Datamuse URL category produced the word |
| Breaking frontend types | Medium | Update types and verify with `npm run build` |

## Definition of Done
- All backend tests pass (336+ existing + new)
- Frontend builds without errors
- Games Manager shows player names, rich event details, relationship types
- EF Core migration created and verified
