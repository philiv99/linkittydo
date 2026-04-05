# Sprint 2 Plan

**Goal**: Create a single-command launcher that starts backend API, frontend dev server, and opens the app in a browser
**Date**: 2026-04-05
**Estimated effort**: ~2 hours

## Selected Items

### 1. One-click app launcher (bat file)
- **Source**: Backlog item #36
- **Tasks**:
  - A. Create `start-app.bat` at repo root that:
    1. Starts the ASP.NET Core backend (`dotnet run`) in a background window
    2. Starts the Vite frontend dev server (`npm run dev`) in a background window
    3. Waits briefly for servers to initialize
    4. Opens `http://localhost:5173` in the default browser
    (~1h)
  - B. Add a companion `stop-app.bat` to cleanly shut down both servers (~0.5h)
  - C. Update `README.md` with usage instructions (~0.5h)

- **Acceptance Criteria**:
  - [ ] Running `start-app.bat` from the repo root starts both backend and frontend
  - [ ] The default browser opens to the frontend URL automatically
  - [ ] Backend API is reachable at `http://localhost:5157`
  - [ ] Frontend dev server runs at `http://localhost:5173`
  - [ ] `stop-app.bat` cleanly terminates both server processes
  - [ ] README documents how to use the launcher
  - [ ] Script handles the case where `npm install` has not been run yet

- **Risk**: Low — straightforward scripting with well-known tools

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Port conflicts if servers already running | Low | Script checks/warns if ports are in use |
| npm dependencies not installed | Low | Script runs `npm install` if `node_modules` missing |

## Definition of Done
- Both bat files exist and work from repo root
- Manual test: run `start-app.bat`, verify both servers start and browser opens
- README updated with quick-start instructions
