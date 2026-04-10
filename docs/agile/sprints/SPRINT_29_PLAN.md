# Sprint 29 Plan

**Sprint**: 29
**Date**: 2026-04-10
**Branch**: `feature/20260410-sprint-29`
**Goal**: Complete remaining admin panel gaps — confirmation dialogs, audit log viewer, CSS cleanup, dashboard refresh

---

## Sprint Items

### Item 1: Confirmation dialogs for destructive actions (#97)
**Priority**: P2
**Acceptance Criteria**:
- Reusable `ConfirmDialog` component with title, message, confirm/cancel buttons
- User deactivation in AdminUsers shows confirmation before executing
- Phrase deactivation in AdminPhrases shows confirmation before executing
- Config changes show confirmation before saving

### Item 2: Admin audit log viewer (#98)
**Priority**: P2
**Acceptance Criteria**:
- Backend: `GET /api/admin/audit-log` endpoint with pagination and optional filters (action type, user, date range)
- Frontend: `AdminAuditLog` page showing log entries with filtering
- Route added at `/admin/audit` with sidebar link
- Displays: timestamp, user, action, details

### Item 3: Admin CSS cleanup — extract inline styles (#99)
**Priority**: P3
**Acceptance Criteria**:
- AdminDashboard, AdminGames, AdminConfig, AdminDataExplorer inline styles extracted to CSS files
- No visual changes to any admin page
- Build passes with no errors

### Item 4: Dashboard refresh and date filters (#100)
**Priority**: P3
**Acceptance Criteria**:
- Manual refresh button on AdminDashboard
- Auto-refresh toggle (30s interval)
- Optional date range selector for dashboard metrics

---

## Estimated Effort
- Item 1: Small (reusable component + 3 integration points)
- Item 2: Medium (backend endpoint + frontend page)
- Item 3: Medium (4 pages of inline style extraction)
- Item 4: Small (refresh logic + minimal UI)
