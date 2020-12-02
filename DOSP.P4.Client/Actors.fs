// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Client

module Actors =

    open Akka.FSharp
    open Akka.Cluster
    open DOSP.P4.Common.Messages.EngineResp
    open DOSP.P4.Common.Messages.User
    open DOSP.P4.Common.Messages.Follow
    open DOSP.P4.Common.Messages.Tweet

    type CFollowCmd = CFollowCmd of FollowType * string
    type CTweet = CTweet of string

    type CTweetQuery =
        | CTweetQueryUser of string
        | CTweetQueryHashTag of string
        | CTweetQueryMention of string

    let getCTweetQueryCmd (u: User) (qt: CTweetQuery) =
        match qt with
        | CTweetQueryUser userName ->
            { TwType = QueryUser
              User = u
              Msg = userName }
        | CTweetQueryHashTag hashTag ->
            { TwType = QueryHashtag
              User = u
              Msg = hashTag }
        | CTweetQueryMention userName ->
            { TwType = QueryMention
              User = u
              Msg = userName }

    let ClientActor (u: User) (mailbox: Actor<obj>) =
        let uRef =
            mailbox.Context.System.ActorSelection("akka.tcp://project4@localhost:8777/user/service-user")

        let fRef =
            mailbox.Context.System.ActorSelection("akka.tcp://project4@localhost:8777/user/service-follow")

        let tRef =
            mailbox.Context.System.ActorSelection("akka.tcp://project4@localhost:8777/user/service-tweet")

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

                | :? CTweet as msg ->
                    match msg with
                    | CTweet tw -> tRef <! TweetTweet u tw

                | :? CTweetQuery as msg -> tRef <! getCTweetQueryCmd u msg

                | :? (EngineResp<obj>) as msg ->
                    match msg.RType with
                    | Succ ->
                        let msgBody = msg.Body
                        match msgBody with
                        | :? TweetCmd as tw -> logErrorf mailbox "Received message %A from %A" tw (mailbox.Sender())
                        | _ -> ()
                    | Fail -> logInfof mailbox "Received message %A from %A" msg.Body (mailbox.Sender())
                //| :? User as msg -> gwRef <! msg
                | :? Tweet as msg -> logInfof mailbox "%s got a tweet from %s: %A" u.Name msg.User.Name msg.Text
                | _ -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                return! loop ()
            }

        loop ()
