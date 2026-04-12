# Sprint 48 Plan — CI ESLint Fixes

**Goal**: Fix all 7 CI eslint errors (react-hooks/set-state-in-effect, unused vars, fast-refresh) to restore green CI.
**Date**: 2026-04-12
**Estimated effort**: ~2 hours

## Selected Items

### 1. Fix setState-in-effect in UserManageModal (Backlog #136)
- **Tasks**:
  - A. Replace useEffect state reset with key-based remount or initializer pattern
- **Acceptance Criteria**:
  - [ ] No `react-hooks/set-state-in-effect` lint error in UserManageModal.tsx
  - [ ] Modal state correctly syncs with props when opened

### 2. Fix setState-in-effect in UserModal + remove unused var (Backlog #137, #138)
- **Tasks**:
  - A. Replace modal reset useEffect with key-based pattern or move reset into open handler
  - B. Convert validation effects (name, email, password) to derive state or use event handlers instead of effects
  - C. Remove unused `_allUsers` destructure and clean up props interface
- **Acceptance Criteria**:
  - [ ] No `react-hooks/set-state-in-effect` lint errors (4 violations fixed)
  - [ ] No `@typescript-eslint/no-unused-vars` error
  - [ ] Name/email debounced availability checks still work
  - [ ] Password validation still works

### 3. Fix fast-refresh in AuthContext (Backlog #139)
- **Tasks**:
  - A. Move `useAuth` hook export to a separate file or suppress with directive comment
- **Acceptance Criteria**:
  - [ ] No `react-refresh/only-export-components` error
  - [ ] `useAuth` import path works across all consumers

### 4. Validation
- **Tasks**:
  - A. Run `npx eslint src/` — 0 errors
  - B. Run `npm run build` — passes
  - C. Run `npm test` — all tests pass
