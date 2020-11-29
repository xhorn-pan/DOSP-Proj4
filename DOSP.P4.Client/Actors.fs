// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Client

module Actors =

    open Akka.FSharp
    open Akka.Cluster
    open DOSP.P4.Common.Messages.User
    open DOSP.P4.Common.Messages.Follow
    open DOSP.P4.Common.Messages.Tweet

    type CFollowCmd = CFollowCmd of FollowType * User
    type CTweet = CTweet of string

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
                        | Follow -> fRef <! FollowUserCmd u following
                        | Unfollow -> fRef <! UnfollowUserCmd u following

                | :? CTweet as msg ->
                    match msg with
                    | CTweet tw -> tRef <! TweetTweet u tw


                //| :? User as msg -> gwRef <! msg
                | _ -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                return! loop ()
            }

        loop ()
