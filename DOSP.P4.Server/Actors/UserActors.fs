// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors

module UserActors =
    open Akka.Actor
    open Akka.FSharp
    open Akka.Cluster
    open Akka.Cluster.Tools
    open Akka.DistributedData
    open MongoDB.Driver
    open MongoDB.FSharp
    open MongoDB.Bson.Serialization
    open DOSP.P4.Common.Utils
    open DOSP.P4.Common.Messages.EngineResp
    open DOSP.P4.Common.Messages.User
    open DOSP.P4.Common.Messages.Follow

    open Common

    type UserSave = UserSave of SUser * IActorRef * IWriteConsistency

    let ignoreIdInFollowCollection () =
        BsonClassMap.RegisterClassMap<FollowCollection>(fun cm ->
            cm.AutoMap()
            cm.SetIgnoreExtraElements(true))
        |> ignore

    let userRegisterActor (mailbox: Actor<SUser * IActorRef>) =
        let uDb = DB.P4GetCollection<SUser> "user"

        let rec loop () =
            actor {
                let! (user, client) = mailbox.Receive()

                try
                    uDb.InsertOneAsync(user).GetAwaiter().GetResult()
                with _ -> client <! RespFail("user exists")

                client
                <! RespSucc(sprintf "user register succ: %A" user)

                return! loop ()
            }

        loop ()

    let userLoginActor (mailbox: Actor<SUser * IActorRef>) =
        let mediator =
            PublishSubscribe.DistributedPubSub.Get(mailbox.Context.System).Mediator

        let fDb =
            DB.P4GetCollection<FollowCollection> "follow"

        let rec loop () =
            actor {
                let! (user, client) = mailbox.Receive()
                // TODO DH key exchange and HMAC thing
                let uid = user.Id

                mediator
                <! PublishSubscribe.Subscribe("mention_" + user.Id.ToString(), client)

                try
                    let followingIds =
                        fDb.FindAsync(fun x -> x.FollowerId = uid).GetAwaiter().GetResult()

                    followingIds.ToEnumerable()
                    |> Seq.iter (fun x ->
                        logErrorf mailbox "sub %s 's tweet by %A" x.UserId client
                        mediator
                        <! PublishSubscribe.Subscribe("tweet_" + x.UserId, client))
                with _ -> client <! RespFail("following not working")

                client <! RespSucc("user login successful")

                return! loop ()
            }

        loop ()

    let userLogoutActor (mailbox: Actor<SUser * IActorRef>) =
        let mediator =
            PublishSubscribe.DistributedPubSub.Get(mailbox.Context.System).Mediator

        let fDb =
            DB.P4GetCollection<FollowCollection> "follow"

        let rec loop () =
            actor {
                let! (user, client) = mailbox.Receive()
                // TODO DH key exchange and HMAC thing
                let uid = user.Id

                mediator
                <! PublishSubscribe.Unsubscribe("mention_" + user.Id.ToString(), client)

                try
                    let followingIds =
                        fDb.FindAsync(fun x -> x.FollowerId = uid).GetAwaiter().GetResult()

                    followingIds.ToEnumerable()
                    |> Seq.iter (fun x ->
                        mediator
                        <! PublishSubscribe.Unsubscribe("tweet_" + x.UserId, client))
                with _ -> client <! RespFail("following not working")

                client <! RespSucc("user logout successful")

                return! loop ()
            }

        loop ()

    let UserActor (mailbox: Actor<UserCmd>) =
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                let user = msg.User

                let client = mailbox.Sender()

                logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                match msg.Cmd with
                | Register -> // from client
                    let uActor =
                        getChildActor "user-register" userRegisterActor mailbox

                    uActor <! (user, client)
                | Login ->
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
