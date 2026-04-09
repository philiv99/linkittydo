# Sprint 14 Retrospective

## Sprint Goal
Add operational health checks and database seeding for development/production environments.

## Delivered
- ASP.NET Core health check infrastructure replacing custom /health endpoint
- JsonStorageHealthCheck — verifies Data/Users, Data/Phrases, Data/GameRecords directories
- MySqlHealthCheck — SELECT 1 connectivity check via EF Core DbContext
- DatabaseSeedService (IHostedService) — seeds admin user in all environments
- Development-only seeding: test user + 5 sample phrases (idempotent)
- Custom health check response writer with uptime, session count, per-check details
- 7 new tests (195 backend total), 57 frontend tests all pass

## Backlog Items
- #67: MySQL health check endpoint — DONE
- #68: Database seeding (admin user, test data) — DONE

## What Went Well
- Clean integration with ASP.NET Core health check middleware
- DatabaseSeedService is idempotent and environment-aware
- Tests use real temp directories for health check validation

## What Could Be Improved
- Test project needed FrameworkReference for Microsoft.AspNetCore.App — discovered at build time

## Lessons Learned
- xUnit test projects targeting net10.0 need explicit `<FrameworkReference Include="Microsoft.AspNetCore.App" />` for ASP.NET Core types like IWebHostEnvironment
