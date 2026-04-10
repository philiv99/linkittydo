# Sprint 27 Retrospective

**Sprint Goal**: Show Admin nav link for users with Admin role (roleId 3) in the main NavHeader.
**Date**: April 10, 2026
**Duration**: Single session
**PR**: #36 (merged to main)

## Delivery Summary

| Planned | Delivered | Carried Over |
|---------|-----------|-------------|
| 1 item (5 tasks) | 1 item (5 tasks) | 0 |

### What Was Delivered
1. **Backend**: Added `Roles` property to `AuthResponse`, role claims in JWT, IRoleService injected into AuthService
2. **Frontend**: Added `roles` to User type, `isAdmin` computed in useUser hook, conditional Admin link in NavHeader
3. **Tests**: 7 new tests (4 NavHeader + 3 AuthService) + 1 pre-existing test fix
4. **Documentation**: Sprint plan, changelog, backlog update, sprint-status.json

### Test Metrics
| Metric | Before | After | Delta |
|--------|--------|-------|-------|
| Backend tests | 290 | 293 | +3 |
| Frontend tests | 57 | 61 | +4 |
| Lint errors | 0 | 0 | 0 |

## What Went Well
- Existing role infrastructure (IRoleService, UserRoles table, admin pages) made this straightforward
- Clean separation between admin auth (adminApi) and player auth (api) prevents token conflicts
- Using `getByRole` in tests avoids ambiguity with element text matching

## What Could Be Improved
- Pre-existing test failures (missing `loginUser` mock) should have been caught in Sprint 26
- The `useUser` hook return type is growing; consider extracting an interface/type for it

## Improvements Applied

| Priority | Improvement | Applied To |
|----------|------------|-----------|
| Low | Note: consider typing useUser return value | Deferred to future sprint |

## Process Notes
- Sprint was compact (single backlog item) and well-scoped
- No blockers encountered
- Auto-merge workflow worked smoothly
