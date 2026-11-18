# ADR-002: EF Core migrations and migration-owned sample data

- **Status:** Accepted
- **Date:** 2026-11-17
- **Decision owner:** User

## Context

SQL Server is the source of truth. The database schema and 20 deterministic sample products must be managed through EF Core migrations, authored during development and applied manually. API startup must not migrate or seed the database.

## Decision

Use the EF Core SQL Server provider and committed migration source files. The initial migration creates `Products` with a GUID primary key and inserts 20 fixed GUID product rows. Developers create migrations with `dotnet ef migrations add`; an operator explicitly applies them to the selected SQL Server with `dotnet ef database update`. API startup performs neither operation.

## Alternatives considered

- Seed on API startup: makes startup mutate the database and can cause multiple processes to race.
- Seed with a separate SQL script: duplicates schema/seed deployment workflows outside EF Core migrations.
- Use integer identity keys: rejected in favor of the user's requested GUID primary key.

## Consequences

- The seed data is versioned and applied once with the migration, and API restarts do not reset product changes.
- Migration commands require a configured SQL Server connection string. The Infrastructure project contains the design-time context factory and migrations; API is the EF CLI startup project, and both projects share a user-secrets ID for design-time configuration.
- Migration application is an explicit operational step; the application does not auto-upgrade the schema.

The migration project placement was amended by [ADR-004](ADR-004-clean-architecture-project-boundaries.md). The original decision to author and apply migrations manually, with API as the startup project, remains unchanged.
