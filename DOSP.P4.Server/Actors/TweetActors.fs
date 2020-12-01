// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors

module TweetActors =
    open Akka.Actor
    open Akka.FSharp
    open Akka.Cluster
    open Akka.Cluster.Tools
    open Akka.DistributedData
    open DOSP.P4.Common.Messages.EngineResp
    open DOSP.P4.Common.Messages.Tweet
    open Common

    let TweetActor (mailbox: Actor<TweetCmd>) =
        let mediator =
            PublishSubscribe.DistributedPubSub.Get(mailbox.Context.System).Mediator

        let db = P4GetCollection<Tweet> "tweet"

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()

                match msg.TwType with
                | NewT
                | RT ->
                    //TODO save tweet
                    let pubT = PubTweet msg
                    let uid = msg.User.Id.ToString()
                    mediator
                    <! PublishSubscribe.Publish("tweet_" + uid, pubT)

                | _ -> ()

                return! loop ()
            }

        loop ()
