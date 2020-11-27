// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils

[<EntryPoint>]
let main argv =
    let config = ConfigurationLoader.load()
    let system = ActorSystem.Create("clusterT", config)
    printfn "wait for join"
    System.Threading.Thread.Sleep 5000

    let echoClient = system.ActorSelection("akka.tcp://clusterT@localhost:8777/user/EchoServer")

    let task = echoClient <? "F#!"

    let response = Async.RunSynchronously (task, 1000)
    printfn "Reply from remote %s" (string(response))
    
    system.WhenTerminated.Wait()
    0 // return an integer exit code