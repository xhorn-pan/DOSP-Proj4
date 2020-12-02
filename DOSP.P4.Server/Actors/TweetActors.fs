// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Server.Actors

open DOSP.P4.Common.Messages.User
open DOSP.P4.Common.Messages.HashTag
open MongoDB.Driver
open DOSP.P4.Common.Messages.Mention

module TweetActors =
    open Akka.Actor
    open Akka.FSharp
    open Akka.Cluster
    open Akka.Cluster.Tools
    open Akka.DistributedData
    open DOSP.P4.Common.Messages.EngineResp
    open MongoDB.Bson
    open DOSP.P4.Common.Messages.Tweet
    open Common

    let getTweet (u: User) (msg: string) =
        let id =
            BsonObjectId(ObjectId.GenerateNewId()).ToString()

        let hts = GetHashTags msg
        let ms = GetMentions msg
        { Id = id
          User = u
          Text = msg
          TwType = NewT
          RtId = ""
          HashTags = hts
          Mentions = ms }

    let tSaveActor (mailbox: Actor<TweetCmd * IActorRef>) =
        let db = P4GetCollection<Tweet> "tweet"

        let rec loop () =
            actor {
                let! (tc, client) = mailbox.Receive()
                let t = getTweet tc.User tc.Msg

                try
                    db.InsertOneAsync(t).GetAwaiter().GetResult()
                with _ -> client <! RespFail("Tweet save failed")

                client <! RespSucc("Tweet save successful")

                return! loop ()
            }

        loop ()

    let tPublishActor (mailbox: Actor<TweetCmd * IActorRef>) =
        let mediator =
            PublishSubscribe.DistributedPubSub.Get(mailbox.Context.System).Mediator

        let rec loop () =
            actor {
                let! (tc, client) = mailbox.Receive()
                //let pubT = PubTweet tc
                let t = getTweet tc.User tc.Msg

                let uid = tc.User.Id.ToString()

                mediator
                <! PublishSubscribe.Publish("tweet_" + uid, t)

                client <! RespSucc("Tweet publish successful")

                return! loop ()
            }

        loop ()

    let tQueryActor (mailbox: Actor<TweetCmd * IActorRef>) =
        let db = P4GetCollection<Tweet> "tweet"

        let rec loop () =
            actor {
                let! (tc, client) = mailbox.Receive()

                let qFilter =
                    match tc.TwType with
                    | QueryUser -> sprintf "{'User.Name': '%s'}" tc.Msg
                    | QueryMention -> sprintf "{Mentions: {$elemMatch: {User: '%s'}}}" tc.Msg
                    | QueryHashtag -> sprintf "{HashTags: {$elemMatch: {Text: '%s'}}}" tc.Msg
                    | _ -> failwith "not query type"

                let filter =
                    qFilter
                    |> BsonDocument.Parse
                    |> BsonDocumentFilterDefinition

                try
                    let twts =
                        db.FindAsync(filter).GetAwaiter().GetResult().ToEnumerable()

                    let resp = twts |> List.ofSeq
                    client <! RespSucc(resp)
                with _ -> client <! RespFail("query error")

                return! loop ()
            }

        loop ()

    let TweetActor (mailbox: Actor<TweetCmd>) =
        let sRef =
            getChildActor "tweet-save" tSaveActor mailbox

        let pRef =
            getChildActor "tweet-publish" tPublishActor mailbox

        let qRef =
            getChildActor "tweet-query" tQueryActor mailbox

        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                let client = mailbox.Sender()

                match msg.TwType with
                | NewT
                | RT ->
                    sRef <! (msg, client)
                    pRef <! (msg, client)

                | QueryUser
                | QueryMention
                | QueryHashtag -> qRef <! (msg, client)
                | _ -> ()

                return! loop ()
            }

        loop ()
