namespace Lmc.Profiler

[<RequireQualifiedAccess>]
module List =
    let takeUpTo limit list =
        if list |> List.length <= limit then list
        else list |> List.take limit
