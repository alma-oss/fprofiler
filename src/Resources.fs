namespace Alma.Profiler

module Resources =
    open Alma.Metrics
    open Alma.ServiceIdentification
    open Alma.State.ConcurrentStorage

    type Resources = Resources of State<Instance, ResourceAvailability>

    let resources = Resources (State.empty())

    [<RequireQualifiedAccess>]
    module Resources =
        let private state (Resources state) = state

        let add resourceType resourceLocation instance =
            let resource =
                ResourceAvailability.createForServiceFromStrings
                    resourceType
                    (instance |> Instance.concat "-")
                    resourceLocation
                    instance
                    (Audience "arch")

            // todo - add resource to metrics

            resources
            |> state
            |> State.set (Key instance) resource

        let values () =
            resources
            |> state
            |> State.items
            |> List.sortByDescending fst
            |> List.map snd
