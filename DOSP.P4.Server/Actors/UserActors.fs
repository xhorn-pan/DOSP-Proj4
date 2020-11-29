// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors

module UserActors =
    open Akka.Actor
    open Akka.FSharp
    open Akka.Cluster
    open Akka.Cluster.Tools
    open Akka.DistributedData
    open DOSP.P4.Common.Messages.EngineResp
    open DOSP.P4.Common.Messages.User
    open Common

    type UserSave = UserSave of User * IActorRef * IWriteConsistency
    let getUserKey (id: int64) = (id / 100L).ToString()

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

                if resp.IsSuccessful
                then client <! RespSucc("user register successful")
                else client <! RespFail("user register error")

                return! loop ()
            }

        loop ()

    let userLoginActor (mailbox: Actor<User * IActorRef>) =
        let replicator =
            DistributedData.Get(mailbox.Context.System).Replicator

        let mediator =
            PublishSubscribe.DistributedPubSub.Get(mailbox.Context.System).Mediator

        let rec loop () =
            actor {
                let! (user, client) = mailbox.Receive()
                // TODO DH key exchange and HMAC thing
                let uid = user.Id

                mediator
                <! PublishSubscribe.Subscribe("mention_" + user.Id.ToString(), client)

                let key =
                    ORSetKey<int64>("uid_fo_" + uid.ToString())

                let rc = ReadLocal.Instance

                let task: Async<IGetResponse> = replicator <? Get(key, rc)

                let resp = Async.RunSynchronously task

                if resp.IsSuccessful then
                    let ids: ORSet<int64> = resp.Get(key)
                    ids //.Elements
                    |> Seq.iter (fun id ->
                        mediator
                        <! PublishSubscribe.Subscribe("tweet_" + id.ToString(), client))

                client <! RespSucc("user login successful")

                return! loop ()
            }

        loop ()

    let userLogoutActor (mailbox: Actor<User * IActorRef>) =
        let mediator =
            PublishSubscribe.DistributedPubSub.Get(mailbox.Context.System).Mediator

        let replicator =
            DistributedData.Get(mailbox.Context.System).Replicator

        let rec loop () =
            actor {
                let! (user, client) = mailbox.Receive()
                // TODO DH key exchange and HMAC thing
                let uid = user.Id

                mediator
                <! PublishSubscribe.Unsubscribe("mention_" + user.Id.ToString(), client)

                let key =
                    ORSetKey<int64>("uid_fo_" + uid.ToString())

                let rc = ReadLocal.Instance

                let task: Async<IGetResponse> = replicator <? Get(key, rc)

                let resp = Async.RunSynchronously task

                if resp.IsSuccessful then
                    let ids: ORSet<int64> = resp.Get(key)
                    ids //.Elements
                    |> Seq.iter (fun id ->
                        mediator
                        <! PublishSubscribe.Unsubscribe("tweet_" + id.ToString(), client))

                client <! RespSucc("user login successful")

                return! loop ()
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
                | Login -> //
                    logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())
                    // DONE NEXT login sub following, should get notify when following user tweet
                    let aRef =
                        getChildActor "user-login" userLoginActor mailbox

                    aRef <! (user, client)
                | Logout ->
                    // logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())
                    let aRef =
                        getChildActor "user-logout" userLogoutActor mailbox

                    aRef <! (user, client)

                return! loop ()
            }

        loop ()
