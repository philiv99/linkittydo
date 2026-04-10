---
name: full-test
description: 'Run all tests and code analysis for both backend and frontend. Use when: validating changes, before PR creation, checking for regressions, running CI checks locally.'
user-invocable: true
---

# Full Test Suite

Run the complete test suite and code analysis for both the ASP.NET Core backend and React frontend.

## When to Use

- Before creating a pull request
- After significant code changes
- To validate no regressions were introduced
- During sprint Phase 4 (Review & Testing)

## Procedure

### 1. Backend Tests

```powershell
cd src/LinkittyDo.Api
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

- All tests must pass
- Note any failures with full error messages

### 2. Frontend Tests

```powershell
cd src/linkittydo-web
npm ci
npm test -- --run
```

- All tests must pass
- Note any failures with full error messages

### 3. Code Analysis

**Backend**:
```powershell
cd src/LinkittyDo.Api
dotnet build --warnaserrors
```

**Frontend** (IMPORTANT: use `npm run build`, not `tsc --noEmit`):
```powershell
cd src/linkittydo-web
npm run build
npx eslint src/
```

- **Always use `npm run build`** as the definitive frontend build check. It runs `tsc -b` which catches unused imports, stricter type checks, and other errors that `tsc --noEmit` misses (learned Sprint 33).
- No compilation warnings or errors
- No lint violations

### 4. Report Results

```
Full Test Results
=================
Backend:
  Build: [Pass/Fail]
  Tests: [X passed, Y failed, Z skipped]
  Warnings: [count]

Frontend:
  Build: [Pass/Fail]
  Tests: [X passed, Y failed]
  TypeScript: [Pass/Fail]
  ESLint: [Pass/Fail]

Overall: [PASS/FAIL]
Issues: [list any failures]
```
