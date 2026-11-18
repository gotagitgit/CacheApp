# Requirements

## Project objective

Build a small .NET 11 Web API that demonstrates layered caching for product data. The API will use HybridCache with an in-process L1 cache and Redis as the distributed L2 cache, with SQL Server as the source of truth.

## Users and use cases

- Developers and learners run the sample locally to observe cache-aside reads, cache invalidation, expiration, stampede protection, and eviction behavior.
- A caller retrieves a product and receives data loaded from SQL Server on a cache miss.
- A caller creates, updates, or deletes a product; subsequent reads return the database's current state.

## Functional requirements

- **FR-01 Product API:** Provide HTTP endpoints to list products with `page` and `pageSize` pagination, retrieve a product by ID, create a product, update a product, and delete a product.
- **FR-02 SQL persistence:** Persist products in SQL Server using Entity Framework Core migrations. SQL Server is the source of truth; cache entries are disposable and can be repopulated.
- **FR-03 Seed data:** Include 20 fixed sample products through an EF Core migration so they become available when database migrations are applied. Seeding must not run as an API startup side effect or reset product data on subsequent migration runs.
- **FR-04 Layered cache:** Use .NET HybridCache with an in-process L1 and Redis-backed L2.
- **FR-05 Cache-aside reads:** For product detail and paginated list reads, check the cache first. On a miss, load the requested result from SQL Server, populate the cache, and return the result. List cache keys must distinguish page and page size.
- **FR-06 Write invalidation:** After a successful product create, update, or delete, invalidate the affected product detail entry and cached product list results, and broadcast the invalidation to connected API processes.
- **FR-07 Expiration:** Apply a short, configurable TTL to product cache entries and list results to limit staleness. The precise default duration is an implementation decision to be documented during design.
- **FR-08 Stampede protection:** Coalesce concurrent cache-miss work for the same key within each API process using HybridCache. A cold miss occurring concurrently in multiple processes may cause one database load per process; distributed locking is not required.
- **FR-09 LRU eviction:** Demonstrate eviction of least-recently-used L1 entries when cache capacity is constrained.
- **FR-10 Local run:** Provide Docker Compose configuration for Redis only. The API runs separately and assumes the Redis container is already running. SQL Server is installed outside Docker, either on the developer's machine or at a remote location.
- **FR-11 Multi-process invalidation:** Support multiple API processes sharing Redis. A successful product write must broadcast cache invalidation so connected processes invalidate their local product detail and list entries. A process that misses a notification must bound stale L1 data by the configured local TTL and invalidate its local product cache before resuming cache reads after Redis reconnects.
- **FR-12 Generic solution identity:** Rename the solution from `ProductCaching` to `CacheApp`, including source and test project names, project directories and files, assembly names, and code namespaces. Keep product domain, API, persistence, and cache behavior unchanged.

## Non-functional requirements

- **NFR-01 Runtime:** Target .NET 11, which the user reports is installed in the development environment.
- **NFR-02 Demonstrability:** Keep the API and cache behavior small and understandable for local experimentation.
- **NFR-03 Data integrity:** During normal Redis connectivity, a successful write propagates product cache invalidation to all connected API processes. If a process misses a notification, its L1 TTL bounds stale local data and reconnect recovery invalidates its local product cache before cache reads resume. If Redis is unavailable during invalidation, the configured L1 and L2 TTLs bound stale values in each layer.
- **NFR-04 Operability:** Redis must be startable using the provided Docker Compose configuration. The API must expose enough health/readiness information for local startup and troubleshooting, including its dependency connections where practical.
- **NFR-05 Configuration:** Do not require secrets to be committed; allow local connection strings and credentials to be configured outside source code.

## Assumptions and constraints

- This is a greenfield standalone sample; no existing application behavior or API contract must be preserved.
- Docker Compose is used only for Redis. Developers run the API separately and provide a connection string for an already-installed local or remote SQL Server.
- The product model will use a small set of ordinary fields appropriate for CRUD demonstration; exact fields and validation will be settled during design.
- LRU refers to L1 eviction under configured capacity pressure. HybridCache's selected in-memory backing store and its support for strict LRU behavior must be confirmed in architecture/design before implementation.
- The exact TTL and cache capacity are tunable demo settings and will be proposed during architecture/design.
- No authentication or authorization requirement has been stated; this is a local demonstration API.

## Out of scope

- Production deployment, high availability, or multi-region operation.
- User accounts, authentication, or authorization.
- A production-grade product catalog, search, paging, or business workflow.
- Cache invalidation across unrelated entities or external systems.
- A graphical user interface.

## Acceptance criteria

- **AC-01:** A developer can start Redis using the provided Docker Compose setup, run the API separately, and connect it to an already-running local or remote SQL Server using configuration outside source code.
- **AC-02:** Product create, detail read, update, and delete endpoints persist and retrieve data from SQL Server.
- **AC-03:** Applying the EF Core migrations to an empty database creates the schema and 20 fixed sample products. Starting or restarting the API does not independently seed or reset product data.
- **AC-04:** The list endpoint returns products by page and page size, with seeded data available across multiple pages.
- **AC-05:** Product detail and paginated list reads use HybridCache L1/L2 and load from SQL Server only after a cache miss; different page/page-size combinations do not share list entries.
- **AC-06:** After a product is created, updated, or deleted successfully, connected API processes receive invalidation and subsequent detail and list reads do not return pre-write cached results.
- **AC-07:** Product and list cache entries expire after the configured short TTL.
- **AC-08:** Concurrent cold reads for the same cache key within one API process produce at most one SQL factory execution. With multiple processes, each process may execute one factory for the same cold key; cross-process locking is not required.
- **AC-09:** With L1 capacity constrained, least-recently-used entries are the eviction candidates, and the behavior can be demonstrated locally.
- **AC-10:** Setup instructions explain how to run the services and exercise the API/cache behavior.
- **AC-11:** With two API processes sharing Redis, a write handled by one process invalidates the corresponding detail entry and all cached list pages in the other connected process.
- **AC-12:** If an API process misses an invalidation notification while running, stale L1 data is bounded by the configured local TTL; when Redis reconnects, that process invalidates its local product cache before cache reads resume. If a process is stopped, its L1 is discarded; on restart it follows the current L2 state, which the writer invalidates when Redis is available.
- **AC-13:** If Redis is unavailable during a write and invalidation cannot be completed, stale cached values are bounded by the configured L1 and L2 expirations; a cache miss reloads the current SQL Server value.
- **AC-14:** The solution and every source/test project use `CacheApp` instead of the `ProductCaching` prefix in their names, paths, assemblies, and namespaces; project references and developer commands resolve correctly. Product API routes, product model, database schema, migrations' behavior, and cache behavior remain unchanged. The renamed solution builds without errors and all available tests pass.

## Phase status

- Requirements Discovery: complete from user-provided answers and documented assumptions.
- Requirements Definition: baseline and multi-process amendment approved by user on 2026-11-17.
- Architecture and Design: approved by user on 2026-11-17.

## Requirements amendment — multi-process caching (2026-11-17)

The user clarified that local execution remains one API process, while the cache design must support multiple API processes for future deployment. This amends FR-06/NFR-03/AC-06 to include cross-process invalidation and adds FR-11/AC-11/AC-12/AC-13 for Redis Pub/Sub propagation, TTL/reconnect recovery, and Redis outage behavior. Stampede protection is explicitly per process, with no distributed lock requirement. The original Requirements Definition approval remains the baseline; the amendment was approved on 2026-11-17. Deployment itself remains out of scope.

- Implementation Planning: approved by user on 2026-11-17.
- Construction: in progress under the approved separate-project architecture; layer restructuring and local build/unit validation are complete, with external SQL Server/Redis integration validation still pending.

## Requirements amendment — generic solution identity (2026-11-18)

The user requested renaming the solution and all project names from `ProductCaching` to `CacheApp` to make the solution identity more generic. They confirmed the scope includes project paths, assemblies, and namespaces. Product-specific domain/API/database concepts and behavior remain unchanged. Requirements Definition was approved on 2026-11-18. The rename was implemented and validated on 2026-11-18: build succeeded, eight tests passed, and one SQL Server/Redis integration test was skipped because its external connections were not configured.
