// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors

open DOSP.P4.Common.Messages.User
open DOSP.P4.Common.Messages.HashTag
open MongoDB.Driver
open DOSP.P4.Common.Messages.Mention
open System

module TweetActors =
    open Akka.Actor
    open Akka.FSharp
    open Akka.Cluster
    open Akka.Cluster.Tools
    open Akka.DistributedData
    open DOSP.P4.Common.Messages.EngineResp
    open MongoDB.Bson
    open DOSP.P4.Common.Messages.Tweet
    open DOSP.P4.Common.Utils
    open Common

    let tSaveActor (mailbox: Actor<Tweet * IActorRef>) =
        let db = DB.P4GetCollection<Tweet> "tweet"

        let rec loop () =
            actor {
                let! (tw, client) = mailbox.Receive()
                // let t = getTweet tc.User tc.Msg
                logErrorf mailbox "save tweet: %A" tw

                try
                    db.InsertOneAsync(tw).GetAwaiter().GetResult()
                with _ -> client <! EngineRespError("Tweet save failed")

                client <! EngineRespSucc("Tweet save successful")

                return! loop ()
            }

        loop ()

    let tPublishActor (mailbox: Actor<Tweet * IActorRef>) =
        let mediator =
            PublishSubscribe.DistributedPubSub.Get(mailbox.Context.System).Mediator

        let rec loop () =
            actor {
                let! (tw, client) = mailbox.Receive()
                let uid = tw.Uid

                mediator
                <! PublishSubscribe.Publish("tweet_" + uid, tw)
                //TODO mentions
                client
                <! EngineRespSucc("Tweet publish successful")

                return! loop ()
            }

        loop ()

    let TweetQueryActor (mailbox: Actor<QueryMsg>) =
        let db = DB.P4GetCollection<Tweet> "tweet"

        let rec loop () =
            actor {
                let! qm = mailbox.Receive()
                let client = mailbox.Sender()

                let qFilter =
                    match qm.QType with
                    | QueryUser -> sprintf "{'Uid': '%s'}" qm.Body
                    | QueryMention -> sprintf "{Mentions: {$elemMatch: {User: '%s'}}}" qm.Body
                    | QueryHashtag -> sprintf "{HashTags: {$elemMatch: {Text: '%s'}}}" qm.Body

                let filter =
                    qFilter
                    |> BsonDocument.Parse
                    |> BsonDocumentFilterDefinition

                try
                    let twts =
                        db.FindAsync(filter).GetAwaiter().GetResult().ToEnumerable()

                    twts |> Seq.iter (fun t -> client <! t)
                // let resp = twts |> List.ofSeq
                // client <! RespSucc(resp)
                with _ -> client <! EngineRespError("query error")

                return! loop ()
            }

        loop ()

    let TweetActor (mailbox: Actor<Tweet>) =
        let sRef =
            getChildActor "tweet-save" tSaveActor mailbox

        let pRef =
            getChildActor "tweet-publish" tPublishActor mailbox

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                let client = mailbox.Sender()

                let nm =
                    { msg with
                          HashTags = HashTag.GetHashTags msg.Text
                          Mentions = Mention.GetMentions msg.Text }
                // save tweet to db
                sRef <! (nm, client)
                // publish tweet to follow
                pRef <! (nm, client)

                return! loop ()
            }

        loop ()
