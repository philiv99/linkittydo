# Sprint 14 Plan

## Goal
Add operational health checks and database seeding for development/production environments.

## Backlog Items
- #67: MySQL health check endpoint — Extend /health with MySQL connectivity check
- #68: Database seeding — Admin user, test data for Development environment

## Tasks
1. Add ASP.NET Core health check infrastructure
2. Create MySQL health check (SELECT 1)
3. Create JSON provider health check (verify data directories exist)
4. Add database seeding service — admin user, sample phrases in Development
5. Wire up health checks in Program.cs
6. Write tests for health checks and seeding
7. Update CHANGELOG
