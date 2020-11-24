// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server

module Actors =
    open System
    open Akka.Actor
    open Akka.Cluster
    open Akka.FSharp
    open Akka.DistributedData
    open DOSP.P4.Common.Messages

    // Write Consistency Policy
    let writeThree = WriteTo(3, TimeSpan.FromSeconds 3.)
    let writeLocal = WriteLocal.Instance

    type UserSave = UserSave of User * IActorRef * IWriteConsistency
    type DBPut = DBPut of Async<IUpdateResponse> * IActorRef

    let DBPutActor (mailbox: Actor<DBPut>) =
        let rec loop () =
            actor {
                let! (DBPut (task, client)) = mailbox.Receive()

                let resp = Async.RunSynchronously task

                logInfof mailbox "send %A to %A" resp (client.Path.ToStringWithAddress())
                // if resp.IsSuccessful then
                client <! resp

                return! loop ()
            }

        loop ()

    let getDBPutChild (mailbox: Actor<_>) =
        let aRef = mailbox.Context.Child("child")
        if aRef.IsNobody() then spawn mailbox "child" DBPutActor else aRef

    let UserActor (mailbox: Actor<UserCmd>) =
        let node =
            Cluster.Get(mailbox.Context.System).SelfUniqueAddress

        let replicator =
            DistributedData.Get(mailbox.Context.System).Replicator

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                match msg.Cmd with
                | Register -> // from client
                    let user = msg.User
                    let client = mailbox.Sender()
                    let wc = writeLocal
                    let key = ORSetKey<string>("user")
                    let set = ORSet.Create<User>(node, user)

                    let task: Async<IUpdateResponse> =
                        replicator
                        <? Update(key, set, wc, (fun old -> old.Merge(set)))

                    let uRepRef = getDBPutChild mailbox
                    uRepRef <! DBPut(task, client)
                | Login -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())
                | Logout -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                return! loop ()
            }

        loop ()

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

                    let uRepRef = getDBPutChild mailbox
                    uRepRef <! DBPut(task, client)
                    return! loop ()
                | Unfollow ->
                    logInfof mailbox "Unfollow is not impl"
                    return! loop ()
            }

        loop ()


    let ServerActor (mailbox: Actor<obj>) =
        let cluster = Cluster.Get(mailbox.Context.System)
        cluster.Subscribe(mailbox.Self, [| typeof<ClusterEvent.IMemberEvent> |])
        mailbox.Defer
        <| fun () -> cluster.Unsubscribe(mailbox.Self)
        logDebugf
            mailbox
            "Created an actor on node [%A] with roles [%s]"
            cluster.SelfAddress
            (String.Join(",", cluster.SelfRoles))

        let rec loop () =
            actor {
                let! (msg: obj) = mailbox.Receive()

                match msg with
                | :? ClusterEvent.IMemberEvent -> logInfof mailbox "Cluster event %A" msg

                | _ -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                return! loop ()
            }

        loop ()
