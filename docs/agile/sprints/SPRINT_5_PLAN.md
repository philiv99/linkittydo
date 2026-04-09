# Sprint 5 Plan: Infrastructure & CI/CD

**Sprint Number**: 5
**Date**: 2026-04-09
**Branch**: `feature/20260409-sprint-5`
**Theme**: CI/CD Pipeline, Session Management, Health Checks, Responsive Layout

---

## Items

### Item 1: CI/CD Pipeline - GitHub Actions (#15)
- Backend: restore, build, test
- Frontend: install, lint, type-check, test, build
- Trigger on PR to main and pushes to main

### Item 2: Session TTL and Cleanup (#16)
- Add configurable session TTL (default 24h) to GameService
- Background timer to purge expired sessions
- Configuration via appsettings.json

### Item 3: Health Check Improvements (#17)
- Expand /health to include dependency status
- Check data directory accessibility
- Report session count and uptime

### Item 4: Responsive Mobile Layout (#11)
- Add responsive breakpoints to GameBoard, PhraseDisplay, CluePanel
- Mobile-friendly word slot sizing
- Ensure NavHeader collapses for small screens

## Acceptance Criteria
- GitHub Actions workflow passes on PR
- Expired sessions are automatically cleaned up
- Health endpoint returns dependency status
- UI works on mobile viewport (375px width)
- All existing tests pass
