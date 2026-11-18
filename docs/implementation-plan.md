# Implementation Plan

## Status

Implementation Planning, including the project-boundary amendment, was approved by the user on 2026-11-17. Construction is in progress. The four-project split builds successfully and unit tests pass; SQL Server/Redis integration validation remains pending because external dependencies are not configured in this environment. The solution identity rename implementation-plan amendment was approved and completed on 2026-11-18: clean build succeeded with zero warnings/errors, formatting passed, eight tests passed, and one SQL Server/Redis integration test was skipped because external connections were not configured.

## Implementation outline

### 1. Reconcile implementation into Clean Architecture projects

- Create `src/CacheApp.Domain`, `src/CacheApp.Application`, and `src/CacheApp.Infrastructure`; retain `src/CacheApp.Api` as the presentation and composition root.
- Enforce the project-reference graph: Domain has no solution-project references; Application references Domain; Infrastructure references Application and Domain; API references Application and Infrastructure.
- Move `Product` and domain invariants to Domain; move product contracts, validation, use cases, and persistence/cache ports to Application; move EF Core, migrations, HybridCache/Redis, Pub/Sub, and cache infrastructure to Infrastructure; keep HTTP endpoints and startup registration in API.
- Refactor the current `ProductService` so Application coordinates operations only through its ports. Infrastructure implements those ports without leaking EF Core or HybridCache types into Application.
- Update test references so unit tests target Domain/Application and integration tests exercise the API host and infrastructure adapters.

### 2. Retain the .NET solution and shared build setup

- Retain `CacheApp.slnx`, shared build configuration, package versions, test projects, and API documentation already created; add the three layer projects and update solution membership/references.
- Keep `global.json` pinned to installed SDK `11.0.100`, `Directory.Build.props`, NuGet Central Package Management, and `.editorconfig` unchanged unless a project move exposes a concrete configuration issue.
- Keep the existing .NET 11, EF Core, HybridCache, Redis, and test package versions centralized. Assign packages to the narrowest project that needs them; Infrastructure owns EF Core, SQL Server, HybridCache, Redis, and Pub/Sub packages, while Application remains free of those implementation packages.
- Keep `Microsoft.EntityFrameworkCore.Design` private to Infrastructure and API: Infrastructure is the migration target, and EF CLI requires the startup project API to reference the design-time package. API has no EF runtime-provider dependency.
- Preserve Swagger/OpenAPI and Problem Details in API so the endpoints remain exercisable locally.

### 3. Keep SQL Server persistence and design-time migrations in Infrastructure

- Move the existing `Product` model to Domain with GUID ID, name, optional description, decimal price, and stock quantity; keep its EF mapping in Infrastructure's `ProductDbContext` with SQL `decimal(18,2)`.
- Add a design-time context factory in Infrastructure that reads the SQL Server connection string from environment configuration or user secrets. Use Infrastructure as the EF target project and API as the startup project.
- Move the existing initial migration and snapshot into Infrastructure without changing the migration ID or its table/seed operations. Confirm its model snapshot still matches the EF model. Do not apply the migration to a database as part of this restructuring, and do not configure startup migration or seed behavior.
- Document the manual migration lifecycle with explicit project arguments: `dotnet ef migrations add <MigrationName> --project src/CacheApp.Infrastructure --startup-project src/CacheApp.Api` creates reviewed source files; `dotnet ef database update --project src/CacheApp.Infrastructure --startup-project src/CacheApp.Api` is run explicitly to apply them to the configured SQL Server.

### 4. Keep the product HTTP API in API and use cases in Application

- Keep the existing Minimal API routes for paginated list, detail, create, update, and delete under `/api/products` in API; make handlers call Application use cases and keep GUID route IDs.
- Use page 1 and page size 10 defaults, reject page values below 1 and page sizes outside 1–50, return results ordered by GUID, and return `{ items, page, pageSize, totalCount }`.
- Validate nonblank name up to 120 characters, optional description up to 500 characters, and nonnegative price and stock quantity. Return Problem Details for invalid input, `404` for missing products, `201` with `Location` on create, and `204` for successful update/delete.
- Keep `/health/live` and `/health/ready` in API; health adapters and dependency checks reside in Infrastructure, with readiness reporting SQL Server, Redis, and invalidation-subscriber readiness.

### 5. Keep HybridCache L1/L2 behavior and invalidation in Infrastructure

- Move the existing HybridCache, MemoryCache with a 20-unit size limit, Redis `IDistributedCache`, and shared long-lived `IConnectionMultiplexer` registrations into Infrastructure; preserve its use for distributed caching, Pub/Sub, and Redis health checks.
- Configure 10-second local and 30-second distributed expiration. Use L1 size 1 for details and the number of returned products (minimum 1) for list entries. Use normal cache priority so capacity compaction chooses LRU entries within the priority bucket.
- Cache details under `products:item:{guid}` and pages under `products:list:{page}:{pageSize}`. Tag every product entry `products`; add `products:list` to every page and `products:item:{guid}` to each detail value.
- Use HybridCache `GetOrCreateAsync`; the factory performs a no-tracking EF query, with list count and page retrieval in the same factory operation. Log the source query in the factory so cache misses are visible.
- On successful create/update/delete, commit SQL first, remove the affected detail key, invalidate `products:list`, and publish a versioned invalidation message containing the product GUID and writer instance ID. A subscriber invalidates the detail key and list tag in its own HybridCache; ignore a message originating from the same instance.
- Run a hosted subscriber before serving requests. On Redis connection restoration, gate cache reads, invalidate the local `products` tag, then restore cache readiness. If invalidation/publication fails after SQL commits, log it and return the successful database result; L1/L2 TTLs bound stale data.
- Retain HybridCache stampede protection per process; do not add a distributed lock.

### 6. Keep Redis Compose setup and operating documentation

- Add a root `compose.yaml` containing Redis only, using the official `redis:7-alpine` image, bound to localhost on port 6379 with a health check and no API or SQL Server services.
- Add configuration examples without credentials; support SQL Server and Redis connection strings through environment variables or user secrets.
- Update README with startup order, Infrastructure-targeted/API-startup EF commands, API routes, multi-process behavior, cache TTL/LRU/stampede demonstrations, and outage limitations.

## Verification plan

- **Project-boundary checks:** Build the solution and verify the project-reference graph matches the architecture: Domain has no project references, Application references only Domain, Infrastructure references Application and Domain, and API references Application and Infrastructure. Confirm Application contains no EF Core, Redis, HybridCache, or ASP.NET Core dependency.
- **Build and static checks:** `dotnet restore`, `dotnet build`, and `dotnet format --verify-no-changes` for the solution.
- **Unit tests:** Validate pagination and product input rules, cache key/tag construction, per-process coalescing, and reconnect invalidation gating.
- **SQL Server integration tests:** Against a dedicated SQL Server database with migrations manually applied, verify schema/seed rows, CRUD persistence, stable page ordering/metadata, and migration-only seed lifecycle.
- **Redis/HybridCache integration tests:** With Redis from Compose, verify L1 hits avoid the factory, L2 serves across separate API hosts, detail and list invalidation after writes, 10/30-second expirations, and LRU eviction under constrained capacity.
- **Cross-process tests:** Start two independently configured API hosts with separate HybridCache L1 instances and shared SQL Server/Redis. Warm both, write through one, and verify the other reloads current data after Pub/Sub invalidation. Simulate a missed event and Redis reconnect; verify local product-tag invalidation before cache reads resume. Verify a stopped host has an empty L1 after restart and uses the current L2 value.
- **Redis outage scenario:** Verify that failed invalidation is logged after a committed SQL write and cached values expire within their configured layer TTLs. Do not assert durable Pub/Sub replay or cross-process distributed stampede suppression.
- **Manual acceptance:** Run Redis Compose, manually apply EF migrations, start one API process, exercise CRUD and pagination with Swagger, then run two API hosts on separate local ports for cross-process invalidation demonstration.

Integration verification requires a Docker-capable environment, a dedicated SQL Server database, and valid local connection strings. The current environment has .NET SDK 11.0.100 but Docker is not available on PATH.

## Dependencies and files

- Root build and run files: `CacheApp.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.config/dotnet-tools.json`, `compose.yaml`, and `README.md`.
- Clean Architecture projects: `src/CacheApp.Domain/`, `src/CacheApp.Application/`, `src/CacheApp.Infrastructure/`, and `src/CacheApp.Api/` with inward project references as described in `docs/architecture.md`.
- EF Core design-time commands target `src/CacheApp.Infrastructure` and use `src/CacheApp.Api` as the startup project; migration files live in Infrastructure.
- Verification: `tests/CacheApp.UnitTests/` and `tests/CacheApp.IntegrationTests/`.
- Architecture records: `docs/decisions/ADR-001-hybridcache-and-redis.md`, `ADR-002-efcore-migrations-and-seed.md`, `ADR-003-cross-process-invalidation.md`, and `ADR-004-clean-architecture-project-boundaries.md`.

## Acceptance mapping

- AC-01 and AC-10: Compose, configuration, and README tasks.
- AC-02 through AC-05: persistence/API/cache-read tasks and SQL/HybridCache integration tests.
- AC-06, AC-11, and AC-12: Pub/Sub subscriber, reconnect recovery, and two-host integration tests.
- AC-07 and AC-13: TTL configuration and expiry/outage tests.
- AC-08: per-process HybridCache stampede unit/integration test; cross-process lock is explicitly excluded.
- AC-09: constrained MemoryCache test that touches hot entries before compaction and verifies LRU candidates.

## Implementation amendment — generic solution identity

Requirements Definition, Architecture and Design, and this implementation-plan amendment for the `CacheApp` identity rename were approved on 2026-11-18.

### Rename tasks

- Rename `ProductCaching.slnx` to `CacheApp.slnx`.
- Rename the four source project directories and `.csproj` files to `CacheApp.Domain`, `CacheApp.Application`, `CacheApp.Infrastructure`, and `CacheApp.Api`; rename the two test projects/directories to `CacheApp.UnitTests` and `CacheApp.IntegrationTests`.
- Update assembly names, root namespaces, source/test `using` directives and namespace declarations, project references, solution paths, API request-file variables, and README/docs commands and paths.
- Update EF migration and snapshot CLR type/namespace references. Keep the migration ID, SQL operations, seed data, table names, and schema unchanged; do not apply migrations to a database.
- Update the relevant ADR project-name references and keep the feature register unchanged because this is a project-wide identity amendment, not a separately owned feature.

### Rename verification

- Restore and build `CacheApp.slnx`; the build must complete without errors.
- Run all available tests with `dotnet test CacheApp.slnx`; the test run must pass. Integration tests requiring unavailable SQL Server/Redis dependencies are expected to follow their existing skip/require-external-dependencies behavior and must be reported accurately.
- Run `dotnet format CacheApp.slnx --verify-no-changes` and inspect the project-reference graph against the approved architecture.
- Search active source, project, solution, and user-facing documentation for remaining `ProductCaching` project prefixes. Product-domain identifiers such as `Product`, `ProductDatabase`, and `Products` remain valid and unchanged.
- Confirm migration IDs and operations are unchanged and the EF model snapshot uses the renamed CLR namespace.

## References

- [EF Core SQL Server 11.0.0](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/11.0.0)
- [HybridCache 11.0.0](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Hybrid/11.0.0)
- [Redis cache adapter 11.0.0](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/11.0.0)
- [StackExchange.Redis 3.2.0](https://www.nuget.org/packages/StackExchange.Redis/3.2.0)
- [ASP.NET Core integration testing 11.0.0](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/11.0.0)
