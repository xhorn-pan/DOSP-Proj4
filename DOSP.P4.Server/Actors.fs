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

    // Read and Write Consistency Policy
    let getUserKey (id: int64) = (id / 100L).ToString()
    let rcPolicy = ReadMajority(TimeSpan.FromSeconds 3.)
    let readLocal = ReadLocal.Instance
    let wcPolicy = WriteMajority(TimeSpan.FromSeconds 3.)
    let writeLocal = WriteLocal.Instance

    type UserSave = UserSave of User * IActorRef * IWriteConsistency
    type DBPut = DBPut of Async<IUpdateResponse> * IActorRef
    type DBGet = DBGet of Async<IGetResponse> * IKey * IActorRef

    let getChildActor name cActor (mailbox: Actor<_>) =
        let aRef = mailbox.Context.Child(name)
        if aRef.IsNobody() then spawn mailbox name cActor else aRef

    let userRegisterActor (mailbox: Actor<User * IActorRef>) =
        let node =
            Cluster.Get(mailbox.Context.System).SelfUniqueAddress

        let replicator =
            DistributedData.Get(mailbox.Context.System).Replicator

        let rec loop () =
            actor {
                let! (user, client) = mailbox.Receive()
                let wc = wcPolicy

                let key =
                    ORSetKey<string>("user-" + (getUserKey user.Id))

                let set = ORSet.Create<User>(node, user)

                let task: Async<IUpdateResponse> =
                    replicator
                    <? Update(key, set, wc, (fun old -> old.Merge(set)))

                let resp = Async.RunSynchronously task

                client <! resp

                return! loop ()
            }

        loop ()

    let userQueryActor (mailbox: Actor<User * IActorRef>) =
        let node =
            Cluster.Get(mailbox.Context.System).SelfUniqueAddress

        let replicator =
            DistributedData.Get(mailbox.Context.System).Replicator

        let rec loop () =
            actor {
                let! (user, client) = mailbox.Receive()
                let rc = rcPolicy

                let key =
                    ORSetKey<string>("user-" + (getUserKey user.Id))

                let task: Async<IGetResponse> = replicator <? Get(key, rc)

                let resp = Async.RunSynchronously task

                logInfof mailbox "send %A to %A" resp (client.Path.ToStringWithAddress())

                let d = resp.Get(key)

                client <! d
            }

        loop ()

    let UserActor (mailbox: Actor<UserCmd>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                let user = msg.User

                let client = mailbox.Sender()

                match msg.Cmd with
                | Register -> // from client
                    let uActor =
                        getChildActor "user-register" userRegisterActor mailbox

                    uActor <! (user, client)
                | Login -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())
                | Logout -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                return! loop ()
            }

        loop ()

    let TweetActor (mailbox: Actor<Tweet>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                match msg.TwType with
                | NewT -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())
                | RT -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())
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

                    let resp = Async.RunSynchronously task
                    client <! resp
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
