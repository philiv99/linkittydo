# Sprint 10 Retrospective

**Sprint Goal**: JWT Authentication
**Date**: 2026-04-09
**Result**: Complete

## Delivered
- JWT authentication with register, login, and refresh token endpoints
- BCrypt password hashing
- Protected user-specific endpoints (update, delete, difficulty, points, games)
- Frontend login/register with password fields and tab UI
- Auth token storage in localStorage with auth headers on protected calls
- 10 new auth tests (173 backend total, 57 frontend total)

## What Went Well
- Clean separation between auth service and existing user service
- All 163 existing tests continued to pass after adding auth
- Frontend TypeScript compilation clean with new auth types

## What Could Improve
- JWT key should use environment variable in production, not appsettings
- Refresh token lookup iterates all users — should add a repository index method

## Key Lesson
Full-stack auth changes require coordinating backend (middleware, controller, service), frontend (API layer, hooks, UI), and tests simultaneously. Planning the data flow end-to-end before coding reduces rework.
