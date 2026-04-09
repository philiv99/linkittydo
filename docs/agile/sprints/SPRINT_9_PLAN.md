# Sprint 9 Plan — Game UI Polish & Content Scaling

**Sprint**: 9
**Date**: 2026-04-09
**Branch**: feature/20260409-sprint-9
**Goal**: Polish the game UI to match design specifications and expand the phrase database for varied gameplay

## Context

Sprints 1-8 delivered the core gameplay loop, routing, clue engine, scoring, accessibility, and security hardening. The game is functionally complete but the UI has remnants from early development (unused HomePage hero section, layout refinements needed) and the phrase database is thin (26 phrases). This sprint polishes the experience before moving to auth/database work.

Research reveals several UI overhaul items (#40, #42) are already implemented. The remaining work is lighter than originally estimated.

## Selected Items

### 1. HomePage Cleanup (#37)
- **Source**: Backlog #37 — Remove splash/click-to-start screens
- **Current state**: Root `/` already redirects to `/play`. GameBoard auto-starts. But `HomePage.tsx` still has a hero section with image overlay and "Play Now" button that is effectively dead code.
- **Tasks**:
  - A. Remove or simplify HomePage component — strip hero section, convert to minimal redirect or remove entirely (~1h)
  - B. Clean up any orphaned CSS and navigate references (~0.5h)
  - C. Update/add tests for simplified routing (~0.5h)
- **Acceptance Criteria**:
  - [ ] No splash screen, hero section, or "Play Now" button exists
  - [ ] Navigating to any route works correctly
  - [ ] No dead code related to old HomePage
- **Risk**: Low — straightforward removal

### 2. Phrase Bar Refinement (#39)
- **Source**: Backlog #39 — Compact phrase bar with action buttons
- **Current state**: Phrase bar has score, timer, streak, and phrase words. Need to verify button layout matches design spec (inline Get Clue, Clue History, Guess buttons per word).
- **Tasks**:
  - A. Audit current phrase bar against design spec — identify gaps (~0.5h)
  - B. Refine button layout for compactness: minimize vertical space, ensure clue/guess actions are discoverable (~2h)
  - C. Ensure "Give Up" button placement is logical and doesn't waste space (~0.5h)
  - D. Test on desktop and mobile viewport widths (~0.5h)
- **Acceptance Criteria**:
  - [ ] Phrase bar occupies minimal vertical space (under 120px on desktop)
  - [ ] Action buttons (Get Clue, Guess) are inline with word slots
  - [ ] Give Up button is accessible but not dominating layout
  - [ ] Responsive on mobile widths
- **Risk**: Low — CSS-focused refinement

### 3. Game Layout & Footer Verification (#38, #41)
- **Source**: Backlog #38, #41 — Professional layout structure & footer
- **Current state**: Layout already uses flex with phrase-bar (compact), clue-area (flex:1), footer (sticky). Footer has version, status, shortcuts. Need to verify and refine.
- **Tasks**:
  - A. Verify clue panel occupies 70-80%+ of viewport — fix if not (~1h)
  - B. Ensure footer is truly minimal height, add game timer to footer if not present (~1h)
  - C. Verify connection status indicator in footer (~0.5h)
  - D. Add tests verifying layout structure renders correctly (~0.5h)
- **Acceptance Criteria**:
  - [ ] CluePanel occupies at least 70% of available viewport
  - [ ] Footer shows: version, connection status, game timer, keyboard shortcuts
  - [ ] Footer height is under 40px
  - [ ] Layout doesn't scroll on standard viewport (1024px+)
- **Risk**: Low

### 4. Scale Phrase Database (#7)
- **Source**: Backlog #7 — Scale phrase database from 26 to 100+
- **Tasks**:
  - A. Generate 80+ new phrases across difficulty bands (easy/medium/hard) via LLM batch generation (~2h)
  - B. Add duplicate detection to GamePhraseService to prevent repeat phrases (~1h)
  - C. Validate all phrases have correct word tagging (hidden vs shown) (~1h)
  - D. Add tests for duplicate detection and phrase variety (~1h)
- **Acceptance Criteria**:
  - [ ] At least 100 total phrases in the database
  - [ ] Phrases span at least 3 difficulty bands
  - [ ] Duplicate detection prevents identical phrases from being added
  - [ ] New phrases follow existing format (stop words, hidden words, categories)
- **Risk**: Medium — LLM generation quality needs manual review; generated phrases may need editing

### 5. Sound Effects Polish (#20) — Stretch Goal
- **Source**: Backlog #20 — Deferred from Sprint 7
- **Tasks**:
  - A. Audit existing audio assets and current integration (~0.5h)
  - B. Add difficulty-appropriate audio cues (correct guess, wrong guess, game complete) (~1.5h)
  - C. Add volume control and mute toggle to settings/footer (~1h)
  - D. Add tests for audio component (~0.5h)
- **Acceptance Criteria**:
  - [ ] Audio feedback on correct/incorrect guesses and game end
  - [ ] Mute toggle accessible in UI
  - [ ] Audio respects prefers-reduced-motion media query
- **Risk**: Low — Enhancement only, no backend changes

## Estimated Effort

| Item | Hours |
|------|-------|
| #37 HomePage Cleanup | 2h |
| #39 Phrase Bar Refinement | 3.5h |
| #38/#41 Layout & Footer | 3h |
| #7 Scale Phrase Database | 5h |
| #20 Sound Effects (stretch) | 3.5h |
| **Total** | **17h** (13.5h without stretch) |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| LLM phrase generation produces low-quality phrases | Medium | Manual review; keep existing 26 as baseline |
| UI changes break existing tests | Low | Run full test suite after each component change |
| Sound effects cross-browser compatibility | Low | Defer to stretch goal; use Web Audio API with fallback |

## Definition of Done
- All acceptance criteria met
- All existing tests pass (157 backend, 55 frontend)
- New tests added for changes
- No lint errors
- Build succeeds (both backend and frontend)
- CHANGELOG.md updated
