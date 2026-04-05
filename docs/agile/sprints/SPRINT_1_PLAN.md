# Sprint 1 Plan

**Goal**: Establish a testing foundation and standardize API responses so all future development is built on verified, consistent code.

**Duration**: April 5, 2026 - April 12, 2026
**Estimated effort**: ~20 hours
**Branch**: `feature/20260405-sprint-1`

---

## Selected Items

### 1. Backend Test Suite (xUnit)
- **Source**: Backlog item #1 (P1 - Critical)
- **Why**: Zero tests exist. Every future sprint depends on regression safety.
- **Tasks**:
  - A. Create `LinkittyDo.Api.Tests` xUnit project, add project reference, wire up solution (~1h)
  - B. Add unit tests for `UserService` - create, get, update, delete, name/email availability, difficulty, points, validation (~3h)
  - C. Add unit tests for `GameService` - start game, submit guess (correct/incorrect), give up, game state, guest vs registered (~3h)
  - D. Add unit tests for `ClueService` - clue retrieval, word index validation (~1h)
  - E. Add unit tests for `GamePhraseService` - phrase selection, phrase count (~1h)
  - F. Add controller tests for `UserController` - all endpoints, error responses, status codes (~2h)
  - G. Add controller tests for `GameController` - start, guess, give-up, record endpoints (~2h)
- **Acceptance Criteria**:
  - [ ] xUnit test project exists and runs via `dotnet test`
  - [ ] All service interfaces have mock-based unit tests
  - [ ] All controller actions have tests covering success and error paths
  - [ ] All tests pass with zero failures
  - [ ] Minimum 30 backend tests
- **Risk**: Low - well-defined service interfaces make mocking straightforward

### 2. Frontend Test Suite (Vitest + Testing Library)
- **Source**: Backlog item #2 (P1 - Critical)
- **Why**: No test framework is installed. Hooks and API service have no verification.
- **Tasks**:
  - A. Install Vitest, @testing-library/react, @testing-library/jest-dom, jsdom; configure vite.config.ts for testing (~1h)
  - B. Add tests for `services/api.ts` - mock fetch, verify all API methods (start game, guess, get clue, user CRUD) (~2h)
  - C. Add tests for `hooks/useGame.ts` - game state management, guess submission, clue retrieval (~1.5h)
  - D. Add tests for `hooks/useUser.ts` - registration, login, localStorage persistence, server sync (~1.5h)
  - E. Add component tests for `GameBoard`, `PhraseDisplay`, `WordSlot` - rendering, user interaction (~2h)
- **Acceptance Criteria**:
  - [ ] Vitest runs via `npm test`
  - [ ] API service has tests for all methods with mocked fetch
  - [ ] Both custom hooks have state management tests
  - [ ] Key components render correctly in tests
  - [ ] All tests pass with zero failures
  - [ ] Minimum 20 frontend tests
- **Risk**: Low - standard Vitest setup for React + TypeScript

### 3. API Response Standardization
- **Source**: Backlog item #3 (P1 - Critical)
- **Why**: Success responses currently return raw DTOs. The spec requires wrapping in `{ data, message }` / `{ error: { code, message } }`. Error responses already use `ErrorResponse` and are mostly compliant.
- **Tasks**:
  - A. Create `ApiResponse<T>` generic wrapper class with `Data` and `Message` properties (~0.5h)
  - B. Update `UserController` - wrap all success returns in `ApiResponse<T>` (~1h)
  - C. Update `GameController` - wrap all success returns in `ApiResponse<T>` (~0.5h)
  - D. Update `ClueController` - wrap success return in `ApiResponse<T>` (~0.5h)
  - E. Update frontend `services/api.ts` to unwrap `data` field from responses (~1h)
  - F. Update all new backend + frontend tests to use the standardized format (~1h)
- **Acceptance Criteria**:
  - [ ] All success responses return `{ "data": {...}, "message": "..." }`
  - [ ] All error responses return `{ "error": { "code": "...", "message": "..." } }`
  - [ ] Frontend api.ts correctly unwraps the `data` field
  - [ ] Swagger shows the wrapper types
  - [ ] No breaking changes to game functionality (manual smoke test)
- **Risk**: Medium - touches all controllers and the frontend API layer. Must coordinate backend + frontend changes. Tests from items #1 and #2 serve as safety net.

---

## Execution Order

The items have internal dependencies and should be executed in this order:

1. **Item #3 first** (API Response Standardization) - changes the contract that tests will verify
2. **Item #1 second** (Backend Tests) - tests validate the standardized API
3. **Item #2 third** (Frontend Tests) - tests validate the frontend against the new response format

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| API wrapper breaks frontend | High | Update frontend api.ts in same commit as backend changes; manual smoke test |
| Mock setup complexity for services with external HTTP calls (ClueService, LlmService) | Medium | Use HttpMessageHandler mocking or interface mocking; skip integration-level HTTP testing |
| Test count targets too aggressive | Low | Prioritize coverage of critical paths; lower count is acceptable if key scenarios are covered |

---

## Definition of Done

- [ ] All backend tests pass (`dotnet test`)
- [ ] All frontend tests pass (`npm test`)
- [ ] Backend builds with no warnings
- [ ] Frontend builds with no errors (`npx tsc --noEmit` + `npx vite build`)
- [ ] ESLint passes (`npx eslint src/`)
- [ ] API responses follow standardized format
- [ ] CHANGELOG.md updated
- [ ] PR created targeting `main`
