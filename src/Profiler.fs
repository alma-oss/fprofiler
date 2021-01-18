namespace Lmc.Profiler

[<RequireQualifiedAccess>]
module Profiler =
    open System
    open Lmc.Metrics
    open Lmc.ServiceIdentification
    open Lmc.EnvironmentModel

    open Lmc.Profiler.Common

    open Queries
    open Resources
    open Errors

    type ApplicationValues = ApplicationValues of Map<Profiler.Label, Profiler.Value>

    let private queryItem color query: Profiler.DetailItem =
        let { Target = (Target (method, Url target)); Created = created } = query.Target

        let shortUrl =
            (target.TrimEnd '/').Split '/'
            |> List.ofArray
            |> List.rev
            |> List.head
            |> (+) "/"

        {
            ShortLabel = Some (Profiler.Label (sprintf "[%A] %s" method shortUrl))
            Label = Profiler.Label target
            Detail = Some (Profiler.ValueDetail query.Response)
            Value = Profiler.Value (created.ToString("HH:mm:ss yyyy-MM-dd"))
            Color = Some color
            Link = Some (Profiler.Link target)
        }

    let private errorItem (message: ErrorMessage, (created: DateTime)): Profiler.DetailItem =
        let shortLabel =
            if message.Length > 50
                then message.Substring(0, 50) + " ..."
                else message

        {
            ShortLabel = Some (Profiler.Label shortLabel)
            Label = Profiler.Label message
            Detail = None
            Value = Profiler.Value (created.ToString("HH:mm:ss yyyy-MM-dd"))
            Color = Some Profiler.Red
            Link = None
        }

    let init currentApplication (ApplicationValues applicationValues) currentEnvironment debug =
        let errorsCount = Errors.count()

        Profiler.Toolbar [
            yield {
                Id = Profiler.Id "Application"
                Label = None
                Value = Profiler.Value (currentApplication |> Box.instance |> Instance.service |> Service.concat "-")
                Unit = None
                ItemColor = Some Profiler.Green
                StatusIcon = None
                Detail = [
                    Profiler.Detail.createItem (Profiler.Label "Instance") (Profiler.Value (currentApplication |> Box.instance |> Instance.concat "-")) |> Profiler.Detail.addColor Profiler.Color.Green
                    Profiler.Detail.createItem (Profiler.Label "Environment") (Profiler.Value (currentEnvironment |> Environment.value))
                    Profiler.Detail.createItem (Profiler.Label "Debug") (Profiler.Value debug) |> Profiler.Detail.addColor (if debug.Contains "Dev" then Profiler.Color.Yellow else Profiler.Color.Green)

                    yield!
                        applicationValues
                        |> Map.toList
                        |> List.map (fun (label, value) ->
                            Profiler.Detail.createItem label value
                        )
                ]
            }

            yield {
                Id = Profiler.Id "Git"
                Label = Some (Profiler.Label "Git")
                Value = Profiler.Value AssemblyVersionInformation.AssemblyMetadata_gitbranch
                Unit = None
                ItemColor = None
                StatusIcon = None
                Detail = [
                    Profiler.Detail.createItem (Profiler.Label "Git Branch") (Profiler.Value AssemblyVersionInformation.AssemblyMetadata_gitbranch)
                    Profiler.Detail.createItem (Profiler.Label "Git Commit") (Profiler.Value AssemblyVersionInformation.AssemblyMetadata_gitcommit)
                ]
            }

            yield {
                Id = Profiler.Id "Resources"
                Label = None
                Value = Profiler.Value "Resources"
                Unit = None
                ItemColor = None
                StatusIcon = None
                Detail =
                    Resources.values ()
                    |> List.choose (function
                        | Service { ResourceAvailability = { Type = resourceType; Location = location } } when (resourceType |> ResourceType.value).Contains "router" ->
                            Profiler.Detail.createItem
                                (Profiler.Label (resourceType |> ResourceType.value))
                                (Profiler.Value (location |> ResourceLocation.value))
                            |> Profiler.Detail.addColor Profiler.Color.Yellow
                            |> Some

                        | Common { Type = resourceType; Location = location }
                        | MultiTenantService { ResourceAvailability = { Type = resourceType; Location = location } }
                        | Service { ResourceAvailability = { Type = resourceType; Location = location } } ->
                            Profiler.Detail.createItem
                                (Profiler.Label (resourceType |> ResourceType.value))
                                (Profiler.Value (location |> ResourceLocation.value))
                            |> Some

                        | _ -> None
                    )
            }

            yield {
                Id = Profiler.Id "Queries"
                Label = None
                Value = Profiler.Value (Queries.count() |> string)
                Unit = Some (Profiler.Unit "Queries")
                ItemColor = None
                StatusIcon = None
                Detail =
                    Queries.values ()
                    |> List.takeUpTo 10
                    |> List.map (function
                        | Query (Ok queryData) -> queryData |> queryItem Profiler.Green
                        | Query (Error queryData) -> queryData |> queryItem Profiler.Red
                    )
            }

            if errorsCount > 0 then
                yield {
                    Id = Profiler.Id "Errors"
                    Label = None
                    Value = Profiler.Value (errorsCount |> string)
                    Unit = Some (Profiler.Unit "Errors")
                    ItemColor = Some (Profiler.Red)
                    StatusIcon = None
                    Detail =
                        Errors.values ()
                        |> List.takeUpTo 10
                        |> List.map errorItem
                }
        ]
