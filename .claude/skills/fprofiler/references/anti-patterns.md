# Anti-Patterns

Each entry is **mistake → why → fix**.

## State & Concurrency

- **Treating the recording modules as request-scoped or isolated.**
  Why: `Queries`, `Errors`, and `Resources` hold process-wide global mutable state shared across all requests and threads.
  Fix: Assume entries from concurrent or prior requests may be present; record promptly and call `Profiler.init` to snapshot, rather than assuming the store reflects only the current request.

- **Trying to construct, reset, or thread the internal `State` value yourself.**
  Why: The `State` backing each module is private and managed internally via `Alma.State.ConcurrentStorage`.
  Fix: Use only the public functions (`add`, `values`, `count`); there is no public reset, so design tests and assertions around accumulating state.

## Counts vs Retained Values

- **Assuming `values` returns every item ever added.**
  Why: Retained history for `Queries` and `Errors` is capped at the last 10 entries, while `count` keeps the true running total.
  Fix: Use `count` for totals and `values` for the (up to) 10 most recent entries; never derive a total from the length of `values`.

- **Expecting more than 10 entries in a toolbar detail panel.**
  Why: `Profiler.init` caps each detail panel to the 10 most recent entries.
  Fix: Treat detail panels as a recent-activity preview, not a full log; surface complete history elsewhere if needed.

## Queries

- **Building `Query` values directly or hand-rolling Ok/Error wrapping.**
  Why: The Ok/Error classification is derived from the `Response`'s `Result` by the library.
  Fix: Wrap the outcome with `Response.create` over a `Result<string,string>` and call `Queries.add`; let the library classify success vs failure.

- **Expecting arbitrary HTTP verbs.**
  Why: `HTTPMethod` is a closed union of `Get`, `Post`, `Put`, and `Delete` only.
  Fix: Map other verbs onto the available cases or extend the library; do not pass a raw method string.

## Resources

- **Assuming resource history is capped or de-duplicated by endpoint.**
  Why: `Resources` is keyed by `Instance` and is not capped; a new registration for an existing instance overwrites the previous one.
  Fix: Register one canonical resource per instance; if you need multiple endpoints, key them by distinct instances.

- **Relying on resources being pushed into a metrics system.**
  Why: Forwarding registered resources to metrics is an open, unimplemented `todo` in the library.
  Fix: Do not assume `Resources.add` emits metrics; if you need metrics, publish them separately via `Alma.Metrics`.

## General

- **Reading the library source to infer the API instead of this skill.**
  Why: The public surface is small and stable; source spelunking wastes context and risks coupling to internals.
  Fix: Use the documented modules and the worked code in `examples.md`.
