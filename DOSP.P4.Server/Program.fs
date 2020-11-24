// Name: Xinghua Pan
// UFID: 95160902
open Akka
open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils
open DOSP.P4.Server.Actors

[<EntryPoint>]
let main argv =
    let config = ConfigurationLoader.load()
    let system = ActorSystem.Create("project4", config)
    // spawn system "server" <| ServerActor |> ignore

    let userRouteConfig = SpawnOption.Router(Routing.FromConfig.Instance)
    //let uRef = 
    spawnOpt system "service-user" UserActor [userRouteConfig] |> ignore

    system.WhenTerminated.Wait()
    0