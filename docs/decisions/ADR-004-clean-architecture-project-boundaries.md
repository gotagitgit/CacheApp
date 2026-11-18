# ADR-004: Separate .NET projects for Clean Architecture layers

- **Status:** Accepted
- **Date:** 2026-11-17
- **Decision owner:** User

## Context

The approved architecture placed API endpoints, product use cases, EF Core persistence, and caching in one API project. During construction, this provided folder organization but did not enforce layer boundaries at compile time. The user has requested a separate project for each Clean Architecture layer.

## Decision

Use four projects: `ProductCaching.Domain`, `ProductCaching.Application`, `ProductCaching.Infrastructure`, and `ProductCaching.Api`.

- Domain contains the product entity and domain invariants and has no references to other solution projects.
- Application contains product use cases, validation and contracts, and the persistence/cache ports required by those use cases; it references Domain only.
- Infrastructure contains EF Core SQL Server persistence and migrations, HybridCache/Redis adapters, Pub/Sub invalidation, and external health checks; it references Application and Domain and implements their ports.
- API contains Minimal API endpoints and the composition root; it references Application and Infrastructure.
- Project dependencies point inward. Application does not reference EF Core, Redis, HybridCache implementations, or ASP.NET Core.

Migrations move to Infrastructure. EF Core CLI commands use Infrastructure as `--project` and API as `--startup-project`, preserving the user's requirement to author migrations with the API startup project and apply migrations manually.

## Alternatives considered

- Keep one API project and use folders: rejected because folders do not enforce compile-time dependency boundaries.
- Combine Domain and Application or Application and Infrastructure: rejected because the user asked for each layer to have its own project.

## Consequences

- Source files, project references, tests, migration location, and migration instructions must follow the project boundaries.
- The existing API behavior, database schema, cache behavior, and accepted product requirements do not change.
- This decision supersedes the single-project choice in the originally approved architecture. The amendment was approved by the user on 2026-11-17; construction resumed after updating the project graph and implementation plan.

## Naming amendment — 2026-11-18

The user approved renaming the solution and projects to `CacheApp` on 2026-11-18. The current names are `CacheApp.Domain`, `CacheApp.Application`, `CacheApp.Infrastructure`, and `CacheApp.Api`; test projects use `CacheApp.UnitTests` and `CacheApp.IntegrationTests`. This supersedes only the project-name portion of the original decision. The four-project boundaries and dependency graph above remain in force. The rename changes source namespaces and assembly names but not HTTP contracts, database schema, migration IDs/operations, or product behavior.
