# ADR-003: Redis Pub/Sub for cross-process cache invalidation

- **Status:** Accepted
- **Date:** 2026-11-17
- **Decision owner:** User

## Context

HybridCache invalidates L1 in the process performing the write but cannot directly clear L1 entries held by other API processes. Multiple processes must promptly invalidate product detail and paginated-list data after a successful write. Local operation still runs one API process; deployment is outside scope.

## Decision

After a successful SQL commit, the writer removes the affected detail key and invalidates the product-list tag in its HybridCache, then publishes a versioned message containing the product GUID and writer instance ID to a Redis Pub/Sub channel. Every API process subscribes and invalidates the corresponding detail key and product-list tag in its own HybridCache; the writer ignores its own broadcast because it already invalidated locally. Every product entry is tagged with `products`; on Redis reconnection, the process invalidates this tag before resuming cache reads. L1 TTL is 10 seconds and L2 TTL is 30 seconds. If invalidation or publication cannot complete after SQL commits, the write remains successful and the failure is logged; configured TTLs bound stale entries.

## Alternatives considered

- HybridCache tag invalidation alone: other processes' in-memory L1 entries are unaffected.
- Durable Redis Streams plus an outbox/replay mechanism: provides recovery of missed events but adds persistence, consumer state, retry, and retention concerns.
- Distributed version check on every read: improves missed-event detection but adds a Redis request to every cache lookup and reduces the L1 latency benefit.

## Consequences

- Connected API processes invalidate promptly without querying SQL for every read.
- Redis Pub/Sub is at-most-once. A process that misses an event may serve its L1 value until the 10-second local expiration or reconnect invalidation. If Redis invalidation itself is unavailable, an L2 value may remain until its 30-second expiration.
- A stopped process loses its L1 cache; it does not need replay if the writer successfully invalidated Redis L2.
- The design provides bounded staleness under disconnection/outage, not durable event replay or strict linearizable reads.
- Redis and the database do not share a transaction. SQL remains authoritative; cache invalidation is best-effort after commit.
