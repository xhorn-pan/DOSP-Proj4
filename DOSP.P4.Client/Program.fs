// Name: Xinghua Pan
// UFID: 95160902

open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils
open DOSP.P4.Common.Messages
open DOSP.P4.Client.Actors
[<EntryPoint>]
let main argv =
    let config = ConfigurationLoader.load()
    let system = ActorSystem.Create("project4", config)
    let client = spawn system "client" ClientActor

    //[1014837012L .. 1014937012L] 
    //[1014837012L .. 1014847012L] 
    // [1014837012L .. 1014838012L] 
    // |> List.iter (fun idx ->
    //     client <! (CreateUser idx "Xhorn.Pan" |> RegisterUser)
    // )
    
    let u1 = CreateUser 1014837012L "Sergey Mutin"
    let t1 = PubTweet u1 "test test test"
    // let u2 = CreateUser 1014837013L "Sergey Mutio"

    // System.Threading.Thread.Sleep 2000
    client <! RegisterUser u1
    client <! t1
    // client <! RegisterUser u2

    // client <! (FollowUserCmd u1 u2)
    // client <! (FollowUserCmd u2 u1)

    system.WhenTerminated.Wait()
    0 // return an integer exit code