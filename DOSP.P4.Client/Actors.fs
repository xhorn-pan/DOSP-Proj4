// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Client

open DOSP.P4.Common.Messages.HashTag

module Actors =
    open System
    open Akka.FSharp
    open Akka.Cluster
    open DOSP.P4.Common.Messages.EngineResp
    open DOSP.P4.Common.Messages.User
    open DOSP.P4.Common.Messages.Follow
    open DOSP.P4.Common.Messages.Tweet
    open MongoDB.Bson

    type CFollowCmd = CFollowCmd of FollowType * string

    type CTweetCmd =
        | CTweet of string
        | CRT of Tweet

    let TweetTweet (u: User) (msg: string) =
        let hts = GetHashTags msg
        { Id = BsonObjectId(ObjectId.GenerateNewId()).ToString()
          User = u
          Text = msg
          CreateAt = DateTime.Now
          ReTweet = false
          RtId = ""
          HashTags = hts
          Mentions = [] }

    let RtTweet (u: User) (t: Tweet) =
        let rtMsg = "@" + t.User.Name

        { Id = BsonObjectId(ObjectId.GenerateNewId()).ToString()
          User = u
          Text = rtMsg
          CreateAt = DateTime.Now
          ReTweet = true
          RtId = t.Id
          HashTags = []
          Mentions = [] }

    let rnd = System.Random()

    type CTweetQuery =
        | CTweetQueryUser of string
        | CTweetQueryHashTag of string
        | CTweetQueryMention of string

    let getCTweetQueryCmd (u: User) (qt: CTweetQuery) =
        match qt with
        | CTweetQueryUser userName -> { QType = QueryUser; Body = userName }
        | CTweetQueryHashTag hashTag -> { QType = QueryHashtag; Body = hashTag }
        | CTweetQueryMention userName ->
            { QType = QueryMention
              Body = userName }
    // akka.tcp://project4@localhost:8777/
    let ClientActor (u: User) (mailbox: Actor<obj>) =
        let uRef =
            mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-user")

        let fRef =
            mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-follow")

        let tRef =
            mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-tweet")

        let qRef =
            mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-query")

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                match msg with
                | :? ClusterEvent.IMemberEvent -> logInfof mailbox "Cluster event %A" msg
                | :? UserCmdType as typ ->
                    match typ with
                    | Register -> uRef <! RegisterUser u
                    | Login -> uRef <! LoginUser u
                    | Logout -> uRef <! LogoutUser u

                | :? CFollowCmd as msg ->
                    match msg with
                    | CFollowCmd (cmd, following) ->
                        match cmd with
                        | Follow -> fRef <! FollowUserIdCmd u.Id following
                        | Unfollow -> fRef <! UnfollowUserIdCmd u.Id following

                | :? CTweetCmd as msg ->
                    match msg with
                    | CTweet tw -> tRef <! TweetTweet u tw
                    | CRT tw -> tRef <! RtTweet u tw

                | :? CTweetQuery as msg -> qRef <! getCTweetQueryCmd u msg

                | :? (EngineResp<obj>) as msg ->
                    match msg.RType with
                    | Succ -> ()
                    | Fail -> logInfof mailbox "Received message %A from %A" msg.Body (mailbox.Sender())

                | :? Tweet as msg -> // from server
                    //logInfof mailbox "%s got a tweet from %s: %A" u.Name msg.User.Name msg.Text
                    logErrorf mailbox "%s got a tweet from %s: %A" u.Name msg.User.Name msg.Text
                    let chance = rnd.Next(100)
                    if chance % 10 = 0 then mailbox.Self <! CRT msg

                | _ -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                return! loop ()
            }

        loop ()
