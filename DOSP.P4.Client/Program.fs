// Name: Xinghua Pan
// UFID: 95160902

open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils
open DOSP.P4.Client.Utils
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

    let numOfUsers = [1000L .. 2000L]

    let users = numOfUsers |> List.map (fun idx ->
        let u = CreateUser ("user-" + idx.ToString())
        let uClient = spawn system ("client-" + idx.ToString()) <| ClientActor u
        uClient <! UserCmdType.Register
        System.Threading.Thread.Sleep 20
        (u.Id, uClient)
    )
    let ucMap = users |> Map.ofList
    let uids = users |> List.map fst
    let ufTable = getZipfFollower uids
    ufTable |> List.iter (fun (uid, followers) -> 
        // printfn "%A folloers: %A" uid followers
        let uClient = ucMap.[uid]
        followers |> Set.iter (fun follower -> 
        uClient <! CFollowCmd(FollowType.Follow, follower)
        )
    )
    System.Threading.Thread.Sleep 10000
    ucMap |> Map.iter (fun _ c ->
        c <!  UserCmdType.Login
    )
    System.Threading.Thread.Sleep 10000
    ucMap |> Map.iter (fun _ c ->
        c <! CTweet (sprintf "test from %s #greeting#cop5615 @root" (c.Path.ToStringWithAddress()))
    )
    // query 
    // System.Threading.Thread.Sleep 20000
    // ucMap |> Map.iter (fun _ c ->
    //     c <! CTweetQueryUser "user-1001"
    //     c <! CTweetQueryHashTag "#greeting"
    //     c <! CTweetQueryHashTag "#cop5615"
    //     // c <! CTweetQueryMention "@root" not working for now
    // )
    system.WhenTerminated.Wait()
    0 // return an integer exit code