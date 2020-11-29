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
    open Common

    let FollowActor (mailbox: Actor<FollowCmd>) =
        let node =
            Cluster.Get(mailbox.Context.System).SelfUniqueAddress

        let replicator =
            DistributedData.Get(mailbox.Context.System).Replicator

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                let client = mailbox.Sender()

                match msg.Cmd with
                | Follow ->
                    let uid = msg.UserId
                    let fid = msg.FollowId
                    let wc = writeLocal

                    let key =
                        ORSetKey<string>("uid_fo_" + uid.ToString())

                    let set = ORSet.Create<int64>(node, fid)

                    let task: Async<IUpdateResponse> =
                        replicator
                        <? Update(key, set, wc, (fun old -> old.Merge(set)))

                    let resp = Async.RunSynchronously task
                    if resp.IsSuccessful then
                        client <! RespSucc("follow successful")
                    else
                        client <! RespFail("follow error")
                    return! loop ()
                | Unfollow ->
                    client <! RespSucc("unfollow not impl")
                    return! loop ()
            }

        loop ()
