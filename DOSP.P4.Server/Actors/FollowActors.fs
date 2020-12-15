// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors

module FollowActors =
    open Akka.Actor
    open Akka.FSharp
    open Akka.Cluster
    open Akka.Cluster.Tools
    open Akka.DistributedData
    open DOSP.P4.Common.Messages.EngineResp
    open DOSP.P4.Common.Messages.Follow
    open DOSP.P4.Common.Utils

    let FollowActor (mailbox: Actor<FollowCmd>) =
        let fDb =
            DB.P4GetCollection<FollowCollection> "follow"

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                let client = mailbox.Sender()

                match msg.Cmd with
                | Follow ->
                    let uf = msg.Col

                    try
                        fDb.InsertOneAsync(uf).GetAwaiter().GetResult()
                    with _ -> client <! EngineRespError("follow error")
                    client <! EngineRespSucc("follow successful")
                    return! loop ()
                | Unfollow ->
                    client <! EngineRespSucc("unfollow not impl")
                    return! loop ()
            }

        loop ()
