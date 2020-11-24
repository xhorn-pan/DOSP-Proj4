// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Client

module Actors =

    open Akka.FSharp
    open Akka.Cluster
    open DOSP.P4.Common.Messages

    let ClientActor (mailbox: Actor<obj>) =
        let uRef =
            mailbox.Context.System.ActorSelection("akka.tcp://project4@localhost:8777/user/service-user")

        let fRef =
            mailbox.Context.System.ActorSelection("akka.tcp://project4@localhost:8777/user/service-follow")

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                match msg with
                | :? UserCmd -> uRef <! msg
                | :? FollowCmd -> fRef <! msg
                | :? ClusterEvent.IMemberEvent -> logInfof mailbox "Cluster event %A" msg
                //| :? User as msg -> gwRef <! msg
                | _ -> logInfof mailbox "Received message %A from %A" msg (mailbox.Sender())

                return! loop ()
            }

        loop ()
