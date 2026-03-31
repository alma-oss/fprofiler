# AGENTS.md — Alma.Profiler

## Project Purpose

`Alma.Profiler` is an F# NuGet library that provides server-side profiler functionality for web applications. It collects and presents runtime diagnostics — application metadata, git info, resource availability, HTTP query history, and error tracking — as a structured `Profiler.Toolbar` data model that the client-side `fable-profiler` renders as a Symfony-style debug toolbar.

## Tech Stack

- **Language:** F# (.NET 10)
- **Package manager:** Paket
- **Build system:** FAKE (F# Make) via `build.sh`
- **NuGet package:** `Alma.Profiler`
- **Repository:** <https://github.com/alma-oss/fprofiler>

## Key Dependencies

- `FSharp.Core ~> 10.0`
- `Alma.EnvironmentModel ~> 10.0` — `Environment`, `Tier` types
- `Alma.Metrics ~> 12.0` — `ResourceAvailability`, `ResourceType`, `ResourceLocation`
- `Alma.Profiler.Common ~> 10.0` — shared types (`Profiler.Toolbar`, `Profiler.Item`, `Profiler.DetailItem`, etc.)
- `Alma.ServiceIdentification ~> 11.0` — `Instance`, `Service`, `Box`
- `Alma.State ~> 11.0` — `ConcurrentStorage.State` for thread-safe mutable collections

## Commands

```bash
# Install dependencies
dotnet paket install

# Build
./build.sh build

# Run tests
./build.sh -t tests
```

## Project Structure

```
├── Profiler.fsproj               # Project file (version, package metadata)
├── AssemblyInfo.fs               # Auto-generated assembly info
├── src/
│   ├── Utils.fs                  # List utilities (filterNotIn, filterNotInBy, takeUpTo)
│   ├── Errors.fs                 # Error tracking — mutable state with capped history
│   ├── Resources.fs              # Resource availability tracking (service endpoints)
│   ├── Queries.fs                # HTTP query recording (target, response, timing)
│   └── Profiler.fs               # Main module — assembles toolbar from all sources
├── build/                        # FAKE build scripts
├── paket.dependencies            # Dependency definitions
├── paket.references              # References for main project
└── fsharplint.json               # Lint config
```

## Architecture

### Modules

1. **`Utils`** — list helpers: `filterNotIn`, `filterNotInBy`, `filterInBy`, `takeUpTo`

2. **`Errors`** — tracks application errors:
   - Mutable `State<DateTime, ErrorMessage * DateTime>` capped at last 10 entries
   - `Errors.add message` / `Errors.values()` / `Errors.count()`

3. **`Resources`** — tracks service resource availability:
   - `State<Instance, ResourceAvailability>` — stores resource endpoints by instance
   - `Resources.add resourceType resourceLocation instance`

4. **`Queries`** — records HTTP queries and responses:
   - Types: `Target` (HTTPMethod * Url), `Response`, `Query` (Ok/Error result)
   - Mutable `State<DateTime * Target, Query>` capped at last 10 entries
   - `Queries.add target response` / `Queries.values()` / `Queries.count()`

5. **`Profiler`** — assembles the toolbar:
   - `Profiler.init currentApplication applicationValues currentEnvironment debug` → `Profiler.Toolbar`
   - Builds toolbar items: Application (instance, environment, debug), Git (branch, commit), Resources, Queries, Errors
   - Each item has detail panels with color-coded entries

### Data Flow

```
Application code → Queries.add / Errors.add / Resources.add (during request handling)
    → Profiler.init (at response time)
    → Profiler.Toolbar (list of Items with detail panels)
    → sent to client → rendered by fable-profiler
```

## Conventions

- **Mutable state modules** — `Errors`, `Queries`, `Resources` use module-level mutable state with `Alma.State.ConcurrentStorage` for thread safety
- **Capped collections** — state is kept at last 10 entries via `State.keepLastSortedBy`
- **Total count tracking** — separate mutable counter for total items (not just last 10)
- **`[<RequireQualifiedAccess>]`** on all public modules
- **Companion library** — this is the server-side counterpart of `fable-profiler` (client-side); they share types via `Alma.Profiler.Common`
- **Color coding** — Green (healthy), Yellow (routers), Red (errors)

## CI/CD

| Workflow | Trigger | What it does |
|---|---|---|
| `tests.yaml` | PR, daily at 03:00 UTC | `./build.sh -t tests` on ubuntu-latest with .NET 10 |
| `publish.yaml` | Tag push (`X.Y.Z`) | `./build.sh -t publish` → NuGet.org |
| `pr-check.yaml` | PR | Blocks fixup commits, runs ShellCheck |

## Release Process

1. Increment `<Version>` in `Profiler.fsproj`
2. Update `CHANGELOG.md`
3. Commit and push a git tag matching the version (e.g., `9.0.0`)

## Pitfalls

- **No docker-compose / no local environment** — this is a pure library, no runtime services
- **No tests directory** — this project appears to have no tests
- **Mutable module state** — `Errors`, `Queries`, `Resources` hold global mutable state; be careful with concurrency and test isolation
- **`takeUpTo` in Utils** — custom implementation; detail panels show at most 10 entries regardless of total count
- **`ResourceAvailability` matching** — `Profiler.init` pattern-matches on `Service`, `Common`, `MultiTenantService` constructors from `Alma.Metrics`; changes to that library may break the toolbar
