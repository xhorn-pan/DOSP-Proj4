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

    let pRouteConfig = SpawnOption.Router(Routing.FromConfig.Instance)
    //let uRef = 
    spawnOpt system "service-user" UserActor [pRouteConfig] |> ignore
    spawnOpt system "service-follow" FollowActor [pRouteConfig] |> ignore
    spawnOpt system "service-tweet" TweetActor [pRouteConfig] |> ignore

    system.WhenTerminated.Wait()
    0