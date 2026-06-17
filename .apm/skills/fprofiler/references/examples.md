# Examples

All code for this skill lives here, ordered from simplest to most complete. Each example is self-contained. Names like `ServiceA`, `WebApi`, and `CacheInstance` are neutral placeholders.

## Tracking an Error

```fsharp
open Alma.Profiler

Errors.add "Upstream returned 503"

let recentErrors = Errors.values ()   // up to 10 newest (ErrorMessage * DateTime), newest first
let totalErrors = Errors.count ()     // running total ever added
```

## Recording a Query

```fsharp
open Alma.Profiler
open Alma.Profiler.Queries

// A successful call to WebApi
let okTarget = Target (Get, Url "https://example.test/web-api/items")
let okResponse = Response.create (Ok "200 OK, 12 items")
Queries.add okTarget okResponse

// A failed call
let failTarget = Target (Post, Url "https://example.test/web-api/items")
let failResponse = Response.create (Error "500 Internal Server Error")
Queries.add failTarget failResponse

let recentQueries = Queries.values ()  // up to 10 newest Query values
let totalQueries = Queries.count ()
```

## Registering a Resource

```fsharp
open Alma.Profiler
open Alma.ServiceIdentification

// instance: an Alma.ServiceIdentification.Instance identifying e.g. CacheInstance
let registerCache (instance: Instance) =
    Resources.add
        "cache"                              // resource type
        "redis://cache.internal.test:6379"   // resource location
        instance

let knownResources = Resources.values ()
```

## Assembling the Toolbar

```fsharp
open Alma.Profiler
open Alma.Profiler.Common
open Alma.ServiceIdentification
open Alma.EnvironmentModel

// currentApplication: Alma.ServiceIdentification.Box
// currentEnvironment: Alma.EnvironmentModel.Environment
let buildToolbar (currentApplication: Box) (currentEnvironment: Environment) =
    // Arbitrary metadata; "Git ..." labels are routed into the Git item.
    let applicationValues =
        Profiler.ApplicationValues [
            Profiler.Label "Git Branch", Profiler.Value "main"
            Profiler.Label "Git Commit", Profiler.Value "a1b2c3d"
            Profiler.Label "Version",    Profiler.Value "9.0.0"
        ]

    // The toolbar snapshots whatever has been recorded via
    // Queries.add / Errors.add / Resources.add so far.
    Profiler.init
        currentApplication
        applicationValues
        currentEnvironment
        "Prod"            // debug string; containing "Dev" colors the Debug entry yellow
```

## Request-Lifecycle Integration

```fsharp
open Alma.Profiler
open Alma.Profiler.Queries
open Alma.ServiceIdentification
open Alma.EnvironmentModel

// Record diagnostics while handling a request, then assemble at the end.
let handleRequest
    (currentApplication: Box)
    (currentEnvironment: Environment)
    (cacheInstance: Instance) =

    // 1. Register a downstream resource ServiceA depends on.
    Resources.add "cache" "redis://cache.internal.test:6379" cacheInstance

    // 2. Record an outgoing call and its outcome.
    let target = Target (Get, Url "https://example.test/service-a/status")
    match (* perform the call *) Ok "200 OK" with
    | Ok body -> Queries.add target (Response.create (Ok body))
    | Error e ->
        Queries.add target (Response.create (Error e))
        Errors.add (sprintf "ServiceA call failed: %s" e)

    // 3. Produce the toolbar to return to the client.
    Profiler.init
        currentApplication
        (Profiler.ApplicationValues [ Profiler.Label "Git Branch", Profiler.Value "main" ])
        currentEnvironment
        "Prod"
```

## Testing Against Accumulating State

```fsharp
open Alma.Profiler

// Global mutable state is shared across tests; assert on relative change.
let ``adding an error increments the total`` () =
    let before = Errors.count ()
    Errors.add "boom"
    let after = Errors.count ()
    assert (after = before + 1)
    // values() is capped at the last 10, so it may not contain every added message
    assert (Errors.values () |> List.isEmpty |> not)
```
