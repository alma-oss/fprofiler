---
name: fprofiler
description: Use whenever generating or reviewing F# server-side code that records request diagnostics or builds a debug toolbar with Alma.Profiler — calling Queries.add, Errors.add, Resources.add, Profiler.init, or constructing a Profiler.Toolbar. Trigger also on mentions of web app profiler, HTTP query history, error tracking, resource availability, ApplicationValues, Target/Response/Query, or rendering a Symfony-style debug toolbar consumed by fable-profiler.
---

# F-Profiler

Library: [alma-oss/fprofiler](https://github.com/alma-oss/fprofiler)
NuGet: `Alma.Profiler`

## Purpose

`Alma.Profiler` is an F# library that collects server-side runtime diagnostics — application metadata, git info, resource availability, HTTP query history, and tracked errors — and assembles them into a `Profiler.Toolbar` data model. It is the server-side counterpart of the client-side `fable-profiler`, which renders the toolbar as a Symfony-style debug bar.

## When to Use

- Recording HTTP queries, errors, or resource endpoints observed during request handling.
- Assembling a profiler toolbar to send to a client for display.
- Reviewing F# code that consumes any of the `Queries`, `Errors`, `Resources`, or `Profiler` modules.

## When NOT to Use

- Pure client-side rendering of the toolbar (that is `fable-profiler`).
- Metrics aggregation, alerting, or persistent storage — this library keeps only short, in-memory history.
- Non-F# consumers, or scenarios needing durable/queryable diagnostics history.

## Main Concepts

- **`Profiler.init`** — entry point; takes the current application, `ApplicationValues`, environment, and a debug string, and returns a `Profiler.Toolbar`.
- **`Profiler.Toolbar`** — the assembled output model (from `Alma.Profiler.Common`); a list of items, each with detail panels.
- **`ApplicationValues`** — wrapper over a `(Label * Value) list` of arbitrary application metadata; entries whose label starts with `git ` are routed into the Git toolbar item.
- **`Queries`** — module with global mutable, capped (last 10) history of recorded HTTP queries; exposes `add`, `values`, `count`.
- **`Target` / `Response` / `Query`** — query value types; `Target` pairs an `HTTPMethod` (Get/Post/Put/Delete) with a `Url`; `Response` wraps `Result<string,string>`; `Query` is the recorded Ok/Error outcome.
- **`Errors`** — module with global mutable, capped (last 10) history of error messages; exposes `add`, `values`, `count`. `ErrorMessage` is a plain `string`.
- **`Resources`** — module tracking service resource availability per `Instance`; exposes `add` and `values` (not capped).
- **`count` vs `values`** — `count` returns the running total of all items ever added; `values` returns only the retained recent items.
- **`List` (Utils)** — augments F# `List` with `filterNotIn`, `filterNotInBy`, `filterInBy`, and `takeUpTo`.

## Related Libraries

- `Alma.Profiler.Common` — shared toolbar types (`Profiler.Toolbar`, `Profiler.Item`, `Profiler.DetailItem`, `Label`, `Value`, `Color`, `Detail`).
- `Alma.Metrics` — `ResourceAvailability`, `ResourceType`, `ResourceLocation`, `Audience`.
- `Alma.ServiceIdentification` — `Instance`, `Service`, `Box`.
- `Alma.EnvironmentModel` — `Environment`.
- `Alma.State` (`ConcurrentStorage`) — thread-safe mutable `State` backing the capped collections.

## Keywords for Search

Alma.Profiler, fprofiler, web app profiler, debug toolbar, Profiler.init, Profiler.Toolbar, ApplicationValues, Queries.add, Errors.add, Resources.add, HTTP query history, error tracking, resource availability, Target, Response, Query, HTTPMethod, ResourceAvailability, git branch, fable-profiler, F# diagnostics

## Reference Files

For composition principles and recommended API usage, read `references/preferred-patterns.md`. For known pitfalls and incorrect assumptions, read `references/anti-patterns.md`. For worked code examples, read `references/examples.md`.
