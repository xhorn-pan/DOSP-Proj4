// Name: Xinghua Pan
// UFID: 95160902

open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils
open DOSP.P4.Common.Messages.User
open DOSP.P4.Common.Messages.Follow
open DOSP.P4.Common.Messages.Tweet
open DOSP.P4.Client.Actors
[<EntryPoint>]
let main argv =
    let config = ConfigurationLoader.load()
    let system = ActorSystem.Create("project4", config)

    System.Threading.Thread.Sleep 5000
    printfn "waiting for node to join cluster"

    [ 1000L .. 1010L]
    |> List.iter (fun idx ->
        let u = CreateUser idx ("user-" + idx.ToString())
        let uClient = spawn system ("client-" + idx.ToString()) <| ClientActor u
        uClient <! UserCmdType.Register
        System.Threading.Thread.Sleep 30
        uClient <! UserCmdType.Login

        uClient <! CTweet "test tweet"
    )

    system.WhenTerminated.Wait()
    0 // return an integer exit code