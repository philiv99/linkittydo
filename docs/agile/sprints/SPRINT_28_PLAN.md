# Sprint 28 Plan

**Goal**: Complete admin functionality — fix build errors, add phrase management, user search/filter, and role management
**Date**: April 10, 2026
**Estimated effort**: 6-8 hours

## Selected Items

### 1. Fix Frontend TypeScript Build Errors (#93)
- **Source**: Backlog item #93 — pre-existing TS errors blocking production build
- **Tasks**:
  - A. Fix `GameBoard.tsx` — add missing `password` property to `RegisterRequest` call (~15min)
  - B. Fix `UserModal.tsx` — remove unused `allUsers` variable (~5min)
  - C. Fix `useUser.ts` — remove unused `getStoredToken` import (~5min)
  - D. Fix `api.test.ts` — resolve `global` not found (~15min)
  - E. Fix `GameBoard.test.tsx` — update mock to include `isAdmin`, `loginUser`, `updateUser`, `resetToGuest`, `fetchAllUsers` (~20min)
  - F. Fix `vite.config.ts` — resolve test config type error (~10min)
  - G. Verify `npm run build` passes cleanly (~5min)
- **Acceptance Criteria**:
  - [ ] `tsc -b` and `npm run build` pass with 0 errors
  - [ ] All existing tests still pass
- **Risk**: Low — straightforward type fixes

### 2. Admin Phrase Management — Backend Endpoints (#102)
- **Source**: Backlog item #102 — backend CRUD for phrases needed by admin UI
- **Tasks**:
  - A. Create `IPhraseAdminService` interface with methods: `GetPhrasesAsync` (paginated), `CreatePhraseAsync`, `UpdatePhraseAsync`, `SetPhraseActiveStatusAsync` (~30min)
  - B. Implement `PhraseAdminService` using EF Core DbContext (~45min)
  - C. Create `PhraseAdminController` with endpoints: `GET /api/admin/phrases`, `POST /api/admin/phrases`, `PUT /api/admin/phrases/{id}`, `PATCH /api/admin/phrases/{id}/status` (~45min)
  - D. Register service and controller in DI (~10min)
  - E. Write unit tests for PhraseAdminService (~30min)
- **Acceptance Criteria**:
  - [ ] `GET /api/admin/phrases?page=1&pageSize=20` returns paginated phrase list
  - [ ] `POST /api/admin/phrases` creates a new phrase
  - [ ] `PUT /api/admin/phrases/{id}` updates phrase text/difficulty
  - [ ] `PATCH /api/admin/phrases/{id}/status` toggles phrase active status
  - [ ] All endpoints require Admin role
  - [ ] Unit tests pass
- **Risk**: Low — follows existing admin controller patterns

### 3. Admin Phrase Management — Frontend Page (#94)
- **Source**: Backlog item #94 — admin UI for phrase browsing and management
- **Tasks**:
  - A. Add phrase admin API methods to `adminApi.ts` (~20min)
  - B. Create `AdminPhrases.tsx` page with paginated phrase table (~45min)
  - C. Add search/filter by difficulty range and active status (~20min)
  - D. Add inline edit/create form for phrases (~30min)
  - E. Add phrase stats display (using existing `phrase-stats` endpoint) (~20min)
  - F. Add route `/admin/phrases` to App.tsx and sidebar nav in AdminLayout (~10min)
  - G. Create `AdminPhrases.css` for styling (~15min)
- **Acceptance Criteria**:
  - [ ] `/admin/phrases` route exists and is protected by AdminGuard
  - [ ] Phrase table shows: text, word count, difficulty, status, created date
  - [ ] Phrases can be searched/filtered
  - [ ] Admin can create new phrases
  - [ ] Admin can edit existing phrases
  - [ ] Admin can activate/deactivate phrases
  - [ ] Phrase play stats are viewable
- **Risk**: Medium — largest UI task, follows existing admin page patterns

### 4. Admin User Search and Simulated Filter (#95)
- **Source**: Backlog item #95 — enhance AdminUsers with search and filter
- **Tasks**:
  - A. Add search input field (name/email) to AdminUsers header (~15min)
  - B. Add backend search parameter to `GET /api/admin/users` if not present (~20min)
  - C. Add simulated user toggle filter to AdminUsers (~10min)
  - D. Wire filters to API calls in adminApi.ts (~15min)
  - E. Add debounced search (~10min)
- **Acceptance Criteria**:
  - [ ] Search input filters users by name or email
  - [ ] Simulated toggle shows/hides bot users
  - [ ] Filters persist across pagination
  - [ ] Search is debounced (300ms)
- **Risk**: Low — extends existing component

### 5. Admin User Role Management (#96)
- **Source**: Backlog item #96 — assign/remove roles from admin panel
- **Tasks**:
  - A. Add backend endpoint `PATCH /api/admin/users/{id}/role` to AdminController (~30min)
  - B. Add `getRoles` and `setUserRole` methods to adminApi.ts (~15min)
  - C. Add role dropdown/badges to AdminUsers row with assign/remove actions (~30min)
  - D. Write backend unit tests for role assignment (~20min)
- **Acceptance Criteria**:
  - [ ] Admin can see current roles for each user
  - [ ] Admin can assign Admin/Moderator/Player roles
  - [ ] Admin can remove roles
  - [ ] Role changes are audit-logged
  - [ ] Backend tests verify role assignment
- **Risk**: Low — backend IRoleService already exists

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Phrase service types mismatch | Low | Follow existing admin service patterns |
| Frontend build errors cascading | Medium | Fix build errors first as item 1 |
| EF Core query performance | Low | Use existing pagination patterns |

## Definition of Done

- All new and existing tests pass
- `dotnet build` succeeds with 0 warnings/errors
- `npm run build` succeeds with 0 TypeScript errors
- All admin pages functional and accessible via sidebar
- CHANGELOG.md updated
