# Sprint 46 Plan — Admin Hard-Delete User

**Sprint Goal**: Add admin ability to permanently (hard) delete a user and all related data from the system.

**Branch**: `feature/20260411-sprint-46`
**Start Date**: 2026-04-11

---

## Backlog Items

| # | Item | Priority |
|---|------|----------|
| 131 | Admin hard-delete user | P1 |

---

## Tasks

| ID | Task | Acceptance Criteria |
|----|------|---------------------|
| 1 | Add `HardDeleteUserAsync` to `IAdminService` and implement in `AdminService` | Method deletes all related data in correct FK order within a transaction: GameEvents → GameRecords → GameSessions → UserRoles → PlayerStats → AuditLog → User. Returns bool. |
| 2 | Add `DELETE /api/admin/users/{uniqueId}` endpoint to `AdminController` | Protected with `[Authorize(Policy = "RequireAdmin")]`. Returns 204 on success, 404 if user not found. Logs audit entry before deletion. |
| 3 | Add unit tests for `AdminService.HardDeleteUserAsync` | Tests: successful delete removes all related data, delete of non-existent user returns false, delete with no related data succeeds, verify FK order (events deleted before records). |
| 4 | Verify backend build passes | `dotnet build` succeeds with 0 errors. |
| 5 | Run full test suite | All existing + new tests pass. |

---

## Data to Delete (FK Order)

1. **GameEvents** — via GameId FK to GameRecords belonging to user
2. **GameRecords** — via UserId FK
3. **GameSessions** — via UserId FK (nullable, so these get cleaned)
4. **UserRoles** — via UserId FK (Restrict delete behavior)
5. **PlayerStats** — via UserId PK
6. **AuditLogEntry** — via UserId (no FK, but clean up for completeness)
7. **User** — the user record itself

---

## Out of Scope

- Frontend admin UI for hard-delete (future sprint)
- Confirmation dialog or soft-delete-first workflow
- Preventing self-deletion (admin deleting themselves)
