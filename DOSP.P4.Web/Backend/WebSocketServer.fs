namespace DOSP.P4.Web.Backend

module WebSocketServer =
    open System
    open WebSharper
    open WebSharper.AspNetCore.WebSocket.Server
    open Akka.Actor
    open Akka.FSharp
    open NSec.Cryptography
    open DOSP.P4.Common.Utils
    open DOSP.P4.Common.Messages.EngineResp
    open DOSP.P4.Common.Messages.User
    open DOSP.P4.Common.Messages.Follow
    open DOSP.P4.Common.Messages.Tweet
    open MongoDB.Bson

    let getEd25519Key () =
        let alg = SignatureAlgorithm.Ed25519
        let priKey = Key.Create alg

        let priKeyExp =
            priKey.Export KeyBlobFormat.PkixPrivateKey

        priKeyExp

    let TweetTweet (msg: string): Tweet =
        { Id = BsonObjectId(ObjectId.GenerateNewId()).ToString()
          Uid = "test_user"
          Text = msg
          CreateAt = DateTime.Now
          ReTweet = false
          RtId = ""
          HashTags = []
          Mentions = [] }

    // TODO add akka.net
    [<JavaScript; NamedUnionCases "c2s">]
    type C2SMessage =
        | Request of str: string
        | [<Name "user-reg">] UserReg of pkey: string // * pubkey: string
        | [<Name "user-login">] UserLogin of uid: string
        | [<Name "user-logout">] UserLogout of uid: string
        | [<Name "user-follow">] UserFollow of uid: string * fid: string
        | [<Name "user-tweet">] UserTweet of tweet: string // all tweet deserialized into string
    // | QTofUser of uid: string
    // | QTofHashTage of hashtag: string
    // | QTofMention of mention: string

    and [<JavaScript; NamedUnionCases "s2c">] S2CMessage = | [<Name "string">] Response of value: string


    let config = ConfigurationLoader.load ()
    let system = ActorSystem.Create("project4", config)

    let wsConnector (client: WebSocketClient<S2CMessage, C2SMessage>) =
        try
            let wsClientActor (client: WebSocketClient<S2CMessage, C2SMessage>) (mailbox: Actor<obj>) =
                let tRef =
                    mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-tweet")

                let uRef =
                    mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-user")

                let fRef =
                    mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-follow")

                let qRef =
                    mailbox.Context.ActorSelection("akka.tcp://project4@localhost:8777/user/service-query")

                let rec loop sender =
                    actor {
                        let! untypeMessage = mailbox.Receive()

                        match untypeMessage with
                        | :? C2SMessage as msg ->
                            match msg with
                            | Request s ->
                                logErrorf mailbox "get msg: %A" s
                                tRef <! TweetTweet s
                                client.PostAsync(Response s) |> Async.Start
                            | UserReg pkey ->
                                let user = SUser.Create pkey
                                uRef <! RegisterUser user

                            | UserLogin uid ->
                                let user = SUser.LogIOU uid
                                uRef <! LoginUser user
                            | UserLogout uid ->
                                let user = SUser.LogIOU uid
                                uRef <! LogoutUser user
                            | UserFollow (uid, fid) -> fRef <! FollowUserIdCmd uid fid
                            | _ ->
                                client.PostAsync(Response "not impl-ed")
                                |> Async.Start
                        | :? (EngineResp<obj>) as msg ->
                            client.PostAsync(Response(sprintf "resp: %A" msg))
                            |> Async.Start
                        | msg ->
                            client.PostAsync(Response(sprintf "catch all resp: %A" msg))
                            |> Async.Start

                        return! loop sender
                    }

                loop None

            spawn system "ws-client" <| wsClientActor client
        with e ->
            printfn "%A" e
            reraise ()

    let Start (): StatefulAgent<S2CMessage, C2SMessage, int> =
        /// print to debug output and stdout
        let dprintfn x =
            Printf.ksprintf (fun s ->
                System.Diagnostics.Debug.WriteLine s
                stdout.WriteLine s) x


        fun client ->
            async {
                let clientIp =
                    client.Connection.Context.Connection.RemoteIpAddress.ToString()

                let aRef = wsConnector client
                return 0,
                       (fun state msg ->
                           async {
                               dprintfn "Received message #%i from %s" state clientIp
                               match msg with
                               | Message data ->
                                   aRef <! data
                                   return state + 1
                               | Error exn ->
                                   eprintfn "Error in WebSocket server connected to %s: %s" clientIp exn.Message
                                   do! client.PostAsync(Response("Error: " + exn.Message))
                                   return state
                               | Close ->
                                   dprintfn "Closed connection to %s" clientIp
                                   return state
                           })
            }
