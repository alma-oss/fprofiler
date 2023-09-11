namespace Alma.Profiler

[<RequireQualifiedAccess>]
module Profiler =
    open System
    open Alma.Metrics
    open Alma.ServiceIdentification
    open Alma.EnvironmentModel

    open Alma.Profiler.Common

    open Queries
    open Resources
    open Errors

    type ApplicationValues = ApplicationValues of (Profiler.Label * Profiler.Value) list

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
        let queriesCount = Queries.count()
        let errorsCount = Errors.count()

        let isGitLabel (Profiler.Label label) =
            label.ToLower().StartsWith "git "

        let gitValues, otherApplicationValues =
            applicationValues
            |> List.partition (fst >> isGitLabel)

        let createDetail kv = kv ||> Profiler.Detail.createItem

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
                        otherApplicationValues
                        |> List.map createDetail
                ]
            }

            yield {
                Id = Profiler.Id "Git"
                Label = Some (Profiler.Label "Git")
                Value =
                    seq {
                        applicationValues
                        |> List.tryFind (fst >> (=) (Profiler.Label "Git Branch"))
                        |> Option.map snd

                        gitValues
                        |> List.tryHead
                        |> Option.map snd

                        Some (Profiler.Value "Git")
                    }
                    |> Seq.pick id

                Unit = None
                ItemColor = None
                StatusIcon = None
                Detail = gitValues |> List.map createDetail
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

            if queriesCount > 0 then
                yield {
                    Id = Profiler.Id "Queries"
                    Label = None
                    Value = Profiler.Value (queriesCount |> string)
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
