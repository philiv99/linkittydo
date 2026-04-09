# Sprint 3 Plan

**Goal**: Implement difficulty-aware clue selection and enhanced scoring so that game difficulty actually affects gameplay
**Date**: 2026-04-05
**Estimated effort**: ~10 hours

## Selected Items

### 1. Difficulty-aware clue selection (Backlog #4, P1)

The `PreferredDifficulty` value is accepted and stored but completely ignored by ClueService. This sprint makes difficulty affect which synonyms and URLs are selected.

- **Tasks**:
  - A. Add `Difficulty` property to `GameSession` so ClueService can access it (~0.5h)
  - B. Update `ClueService.GetClueAsync()` to accept difficulty parameter (~0.5h)
  - C. Implement difficulty-tiered synonym selection (~2h):
    - Easy (0-20): Prefer close synonyms (`rel_syn`), pick top-ranked results
    - Medium (21-50): Mix of `rel_syn` and `ml`, mid-range results
    - Hard (51-80): Prefer distant synonyms (`ml`), triggers (`rel_trg`), lower-ranked results
    - Expert (81-100): Most distant associations, obscure synonyms only
  - D. Implement difficulty-tiered URL preference (~1h):
    - Easy: Prefer transparent URLs (Wikipedia, dictionary sites)
    - Medium: No URL preference filtering
    - Hard/Expert: Prefer opaque/generic URLs (skip Wikipedia/dictionary)
  - E. Add unit tests for difficulty-aware clue selection (~1.5h)

- **Acceptance Criteria**:
  - [ ] `GameSession` stores the difficulty value from `StartGameRequest`
  - [ ] `ClueService.GetClueAsync()` receives and uses difficulty
  - [ ] Easy difficulty produces closer synonyms than hard difficulty
  - [ ] URL selection strategy varies by difficulty tier
  - [ ] Existing ClueService tests still pass
  - [ ] New tests cover all 4 difficulty tiers

- **Risk**: Medium — Datamuse may not always return enough results to differentiate tiers; fallback logic needed

### 2. Enhanced scoring model (Backlog #5, P1)

Replace flat 100-point scoring with the design formula: `WordScore = BasePoints / (n_clues × n_guesses)`.

- **Tasks**:
  - A. Track per-word clue count and guess count in `GameSession` (~1h)
  - B. Implement scoring formula in `GameService.SubmitGuessAsync()` (~1h):
    - BasePoints by difficulty: Easy=100, Medium=150, Hard=200, Expert=300
    - Formula: `BasePoints / (clueCount × guessCount)` (minimum 1 for each)
    - First-guess bonus: 2x multiplier when correct on first guess with no clues
  - C. Update `GuessEvent.PointsAwarded` to use the new calculated value (~0.5h)
  - D. Add unit tests for scoring formula with various scenarios (~1h)

- **Acceptance Criteria**:
  - [ ] Points awarded vary based on number of clues requested and guesses made
  - [ ] Higher difficulty yields higher base points
  - [ ] First-guess-no-clue bonus of 2x is applied correctly
  - [ ] Score is rounded to nearest integer
  - [ ] `GameRecord.Score` reflects the sum of per-word scores
  - [ ] Tests cover: easy/hard scoring, multi-clue penalty, first-guess bonus, edge cases

- **Risk**: Low — pure formula logic with no external dependencies

### 3. Phrase difficulty computation (Backlog #6, P2)

Add a `Difficulty` field to `GamePhrase` and compute a basic difficulty score.

- **Tasks**:
  - A. Add `Difficulty` property (int, 0-100) to `GamePhrase` model (~0.5h)
  - B. Propagate `Difficulty` to `Phrase` model used in game sessions (~0.5h)
  - C. Implement `ComputePhraseDifficulty()` in `GamePhraseService` (~1h):
    - Based on: hidden word count ratio, phrase word count, average word length of hidden words
    - Normalize to 0-100
  - D. Backfill difficulty scores for existing 26 phrases (migration on load) (~0.5h)
  - E. Update phrase selection to prefer phrases near user's preferred difficulty (~0.5h)
  - F. Add unit tests for difficulty computation (~1h)

- **Acceptance Criteria**:
  - [ ] `GamePhrase` has `Difficulty` property persisted in JSON
  - [ ] Existing phrases get difficulty scores on first load (graceful migration)
  - [ ] Short common phrases score lower difficulty than long obscure ones
  - [ ] Phrase selection prefers phrases within ±20 of user's preferred difficulty
  - [ ] Tests validate computation for various phrase types

- **Risk**: Low — heuristic-based, no external API needed; can refine formula later

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Datamuse returns too few results for hard-mode filtering | Medium | Stack multiple query types (`ml`, `rel_trg`); fall back to any synonym if filtered set is empty |
| Difficulty tier boundaries feel arbitrary | Low | Start with 4 tiers; tune after playtesting in later sprints |
| Phrase difficulty heuristic is imprecise | Low | Use simple metrics now; refine with gameplay data in Sprint 9 |

## Definition of Done
- All 3 items implemented and tested
- All existing backend tests pass (70+)
- New tests added for difficulty tiers, scoring formula, phrase difficulty
- Frontend continues to work (difficulty value already sent from UI)
- No regressions in `dotnet build` or `npx vite build`
- CHANGELOG.md updated
