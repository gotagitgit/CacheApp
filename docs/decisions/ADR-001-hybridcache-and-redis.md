# ADR-001: HybridCache with Redis L2 and MemoryCache L1

- **Status:** Accepted
- **Date:** 2026-11-17
- **Decision owner:** User

## Context

The demo must show cache-aside product reads with L1 and L2 layers, cache stampede protection, short expirations, and LRU eviction. It must also support multiple API processes sharing Redis, while local operation uses one API process.

## Decision

Use .NET HybridCache with the default in-process MemoryCache as L1 and Redis through `IDistributedCache` as L2. Use `GetOrCreateAsync` for detail and paginated list cache-aside reads. Set proposed defaults of 10 seconds for L1 and 30 seconds for L2. Configure bounded L1 size using .NET 11 HybridCache local sizing and MemoryCache capacity compaction. Rely on HybridCache's per-process stampede coalescing; do not add a distributed lock.

## Alternatives considered

- Hand-coordinate `IMemoryCache` and Redis: duplicates cache orchestration and stampede handling.
- Redis-only cache: omits the requested in-process L1.
- Redis distributed lock: reduces cold-miss SQL loads across processes but adds lock lease, timeout, and failure-recovery behavior beyond this demo's requirement.

## Consequences

- L1 hits avoid Redis and SQL access; L2 is shared by API processes.
- Stampede coalescing is per process. With multiple processes, a simultaneous cold miss can cause one source query per process.
- MemoryCache compaction selects least-recently-used entries within priority buckets after expired entries; this is LRU compaction rather than a strict fixed-size LRU data structure.
- L1 is process-local and disappears when a process stops. Cache entries are disposable and repopulate from SQL Server.
