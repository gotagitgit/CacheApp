# Product Caching Demo API

A .NET 11 Minimal API demonstrating product cache-aside reads with HybridCache L1, Redis L2, SQL Server persistence, and cross-process invalidation.

## Requirements

- .NET SDK 11.0.100 or a later 11.0 patch.
- Docker with Docker Compose for Redis.
- SQL Server installed locally or reachable remotely.

The default local workflow runs one API process. The Redis Pub/Sub invalidation design also supports multiple API processes sharing the same Redis instance.

## Start Redis

Docker Compose starts Redis only; the API and SQL Server run outside Docker.

```powershell
docker compose up -d redis
```

Redis listens on `localhost:6379`. Confirm it is healthy with:

```powershell
docker compose ps
```

## Configure SQL Server

Configure the SQL Server connection string with .NET user secrets. The API and Infrastructure projects share the same user-secrets ID for runtime and EF design-time configuration:

```powershell
dotnet user-secrets set "ConnectionStrings:ProductDatabase" "Server=localhost;Database=ProductCaching;Trusted_Connection=True;TrustServerCertificate=True;" --project src/CacheApp.Api
```

Replace the connection string for a remote SQL Server. You can also use the environment variable `ConnectionStrings__ProductDatabase`. The Redis connection defaults to `localhost:6379,abortConnect=true`; override it with `ConnectionStrings__Redis` when needed. Do not commit credentials.

## Create and apply database migrations

Restore the repository-local EF Core tool, then create migration files during development:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> --project src/CacheApp.Infrastructure --startup-project src/CacheApp.Api --output-dir Data/Migrations
```

Review and commit the generated migration files. The initial migration creates the `Products` table and inserts the 20 fixed GUID sample products.

Applying migrations is a separate manual operation. It changes the configured database:

```powershell
dotnet ef database update --project src/CacheApp.Infrastructure --startup-project src/CacheApp.Api
```

The API does not apply migrations or seed data on startup. Existing product data is not reset when the API restarts.

## Project structure

- `CacheApp.Domain` contains the product entity and domain rules.
- `CacheApp.Application` contains product use cases, contracts, validation, and persistence/cache ports. It references only Domain.
- `CacheApp.Infrastructure` implements the persistence/cache ports and contains EF Core migrations, SQL Server, HybridCache, Redis, and Pub/Sub adapters. It references Application and Domain.
- `CacheApp.Api` contains HTTP endpoints and the composition root. It references Application and Infrastructure. Its EF Core Design package is private and present only so EF CLI can use API as its startup project.

Project dependencies point inward. The API project does not reference EF Core runtime or Redis provider packages.

## Run the API

```powershell
dotnet run --project src/CacheApp.Api
```

In Development, Swagger UI is available at `http://localhost:5068/swagger`. Readiness is at `/health/ready`; liveness is at `/health/live`.

## Product endpoints

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/products?page=1&pageSize=10` | List products; page size is 1–50 |
| `GET` | `/api/products/{guid}` | Get a product |
| `POST` | `/api/products` | Create a product |
| `PUT` | `/api/products/{guid}` | Update a product |
| `DELETE` | `/api/products/{guid}` | Delete a product |

Create/update request body:

```json
{
  "name": "USB-C Dock",
  "description": "Dual display dock",
  "price": 89.95,
  "stockQuantity": 12
}
```

## Observe the cache

For a detailed walkthrough of the L1/L2 cache setup, key/tag layout, and cross-process invalidation flow, see [Hybrid cache reference](references/hybrid-cache.md).

- Product details use `products:item:{guid}`. Each page uses `products:list:{page}:{pageSize}`.
- HybridCache checks local MemoryCache first, then Redis, then SQL Server on a miss. SQL factory loads are logged.
- L1 expires after 10 seconds; Redis L2 expires after 30 seconds. These values and the 20-unit L1 capacity are configurable in `appsettings.json` or environment variables.
- Product writes remove the affected detail entry, invalidate the `products:list` tag, and publish the product ID through Redis Pub/Sub. Connected API processes invalidate their local entries.
- A disconnected process can miss Pub/Sub and retain stale L1 data until its local TTL expires. On Redis reconnect, the process clears its local product cache before cache reads resume. If Redis is unavailable during a write, stale L2 data may remain until its 30-second TTL expires.
- HybridCache coalesces concurrent cache misses within each API process. Multiple processes may each issue one SQL query for the same simultaneous cold miss; no distributed lock is used.
- L1 capacity compaction uses MemoryCache's least-recently-used ordering within priority buckets. To observe it, constrain `Cache:LocalSizeLimit`, read several different details/pages, revisit some entries to make them more recent, then create additional pressure and inspect the miss-load logs.

To try multiple processes locally, start the API twice on different ports while sharing the same SQL Server and Redis connection settings:

```powershell
dotnet run --project src/CacheApp.Api --urls http://localhost:5068
dotnet run --project src/CacheApp.Api --urls http://localhost:5069
```

## Tests

```powershell
dotnet test CacheApp.slnx
```

Unit tests run without external services. The cross-process integration tests require a Docker-compatible container runtime and a SQL Server connection string in `ConnectionStrings__TestSqlServer`. The SQL connection must target `master` (or another system database) and use a test-only login that can create and drop databases; never point it at production.

For example, in PowerShell, configure the connection for the current terminal session and run the suite:

```powershell
$env:ConnectionStrings__TestSqlServer = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet test CacheApp.slnx
```

### Run integration tests from Visual Studio Test Explorer

Visual Studio can pass the SQL connection string to the test process through a `.runsettings` file. For local development, create `CacheApp.local.runsettings` in the solution root (this file is ignored by Git) with your test SQL Server connection string:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <EnvironmentVariables>
      <ConnectionStrings__TestSqlServer>Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;</ConnectionStrings__TestSqlServer>
    </EnvironmentVariables>
  </RunConfiguration>
</RunSettings>
```

In Visual Studio, select **Test > Configure Run Settings > Select Solution Wide runsettings File** and choose `CacheApp.local.runsettings`. Then run or debug the integration test in Test Explorer. Make sure Docker is running for the Redis container. Replace the example SQL connection string with your local test SQL Server settings; do not commit connection strings containing credentials.

The integration-test fixture starts one isolated Redis container for its xUnit collection and creates a uniquely named `ProductCachingTest_<id>` database on the configured SQL Server instance. It applies the EF Core migrations, runs the test, then drops the temporary database and removes the Redis container when the fixture ends. Both API hosts in the test share that fixture's SQL database, Redis endpoint, cache key prefix, and Pub/Sub channel. They do not connect to or affect the Redis container used for local debugging. If the test process is forcibly terminated before cleanup, a temporary SQL database may remain and need manual removal.
