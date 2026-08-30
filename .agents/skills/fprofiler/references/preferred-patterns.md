# Preferred Patterns

## Core Principles

- **Record during the request, assemble at the end.** Call `Queries.add`, `Errors.add`, and `Resources.add` as events occur while handling a request, then call `Profiler.init` once at response time to produce the `Profiler.Toolbar`.
- **The recording modules own their state.** `Queries`, `Errors`, and `Resources` hold module-level state internally — you push facts in and read them back; you never construct or thread a state value yourself.
- **Let labels drive grouping.** Application metadata passed through `ApplicationValues` is split by label: any entry whose label starts with `git ` (case-insensitive) is surfaced in the Git toolbar item; everything else appears under the Application item.

## Recommended API Usage

- **Errors** — `Errors.add` takes a message string; `Errors.values` returns retained `(ErrorMessage * DateTime)` entries newest-first; `Errors.count` returns the running total.
- **Queries** — build a `Target` from an `HTTPMethod` and `Url`, wrap the outcome with `Response.create` over a `Result<string,string>`, then call `Queries.add target response`. `Query.ofResponse` is the internal bridge that turns a `Response` into an Ok/Error `Query`; prefer `Queries.add` rather than building `Query` values by hand. See `examples.md` → Recording a Query.
- **Resources** — `Resources.add resourceType resourceLocation instance` registers an endpoint for an `Instance`; later registrations for the same instance overwrite earlier ones. See `examples.md` → Registering a Resource.
- **Profiler** — `Profiler.init currentApplication applicationValues currentEnvironment debug` returns the toolbar. The Queries and Errors items are emitted only when their count is greater than zero; detail panels show at most the 10 most recent entries. See `examples.md` → Assembling the Toolbar.

## Error Handling

- A query is recorded as a failure by passing an `Error` case inside the `Response`'s `Result`; this colors its toolbar entry red. A successful query uses the `Ok` case and is colored green.
- `Errors.add` is for application-level error messages surfaced in the toolbar's Errors item; it is independent of failed queries.

## Composition

- The output is a plain immutable `Profiler.Toolbar` value (a list of items). Treat it as data: serialize it and hand it to the client; do not mutate it after `Profiler.init`.
- Toolbar items map to fixed ids (`Application`, `Git`, `Resources`, `Queries`, `Errors`); rely on these stable ids rather than item ordering when consuming the toolbar downstream.

## Integration with Other Libraries

- `Profiler.init` reads identity from `Alma.ServiceIdentification` (`Box.instance`, `Instance`, `Service`) and the environment from `Alma.EnvironmentModel` (`Environment.value`).
- `Resources` builds `Alma.Metrics` `ResourceAvailability` values; `Profiler.init` pattern-matches on `Service` / `Common` / `MultiTenantService` resource shapes, treating any `ResourceType` containing `router` as a yellow entry.
- The toolbar model itself comes from `Alma.Profiler.Common`; reuse its `Label`, `Value`, `Color`, and `Detail` helpers when extending application values.

## Naming Conventions

- All public modules use `[<RequireQualifiedAccess>]`, so always call qualified: `Queries.add`, `Errors.values`, `Resources.add`, `Profiler.init`.
- Git-related application values follow the `Git <name>` label convention (e.g. `Git Branch`, `Git Commit`) so they group correctly.

## Testing Recommendations

- The recording modules use global mutable state, so tests are not isolated by default. Account for residual entries from earlier tests, and prefer asserting on relative changes in `count` rather than absolute values.
- Because retained history is capped at the last 10 entries while `count` keeps the true total, assert these two independently.
