// Name: Xinghua Pan
// UFID: 95160902
open Akka
open Akka.Actor
open Akka.FSharp
open Akka.Configuration
open DOSP.P4.Common.Utils
open DOSP.P4.Server.Actors.UserActors
open DOSP.P4.Server.Actors.FollowActors
open DOSP.P4.Server.Actors.TweetActors


[<EntryPoint>]
let main argv =
    let config = 
        if Array.contains "--seed" argv then 
            Configuration.parse("""
                akka {
                    remote {
                        dot-netty.tcp {
                            port=8777
                        }
                    }
                    cluster {
                        roles = [seed, server]
                    }
                }""").WithFallback(ConfigurationLoader.load())
        else
            ConfigurationLoader.load()

    let system = ActorSystem.Create("project4", config)
    if Array.contains "--seed" argv then 
        let pRouteConfig = SpawnOption.Router(Routing.FromConfig.Instance)
        //let uRef = 
        spawnOpt system "service-user" UserActor [pRouteConfig] |> ignore
        spawnOpt system "service-follow" FollowActor [pRouteConfig] |> ignore
        spawnOpt system "service-tweet" TweetActor [pRouteConfig] |> ignore
        spawnOpt system "service-query" TweetQueryActor [pRouteConfig] |> ignore
        
    ignoreIdInFollowCollection()
    system.WhenTerminated.Wait()
    0