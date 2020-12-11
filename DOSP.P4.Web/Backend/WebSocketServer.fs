namespace DOSP.P4.Web.Backend

module WebSocketServer =

    open WebSharper
    open WebSharper.AspNetCore.WebSocket.Server
    open Akka.Actor
    open Akka.FSharp
    open DOSP.P4.Common.Utils
    open DOSP.P4.Common.Messages.User
    open NSec.Cryptography

    let getEd25519Key () =
        let alg = SignatureAlgorithm.Ed25519
        let priKey = Key.Create alg

        let priKeyExp =
            priKey.Export KeyBlobFormat.PkixPrivateKey

        priKeyExp

    // TODO add akka.net
    [<JavaScript; NamedUnionCases>]
    type C2SMessage = Request of str: string

    and [<JavaScript; NamedUnionCases "type">] S2CMessage = | [<Name "string">] Response of value: string


    let config = ConfigurationLoader.load ()
    let system = ActorSystem.Create("project4", config)

    let wsConnector (client: WebSocketClient<S2CMessage, C2SMessage>) =
        try
            let wsClientActor (client: WebSocketClient<S2CMessage, C2SMessage>) (mailbox: Actor<C2SMessage>) =
                let rec loop sender =
                    actor {
                        let! untypeMessage = mailbox.Receive()

                        match untypeMessage with
                        | Request s ->

                            let j: obj = s |> Json.Deserialize
                            match j with
                            | :? WSServerUser as user -> logErrorf mailbox "get user reg: %A" user
                            | _ -> logErrorf mailbox "get msg: %A" j

                            client.PostAsync(Response s) |> Async.Start

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

                // let uid =
                //     client.Context.RequestUri.Query.Split('=').[1]

                // dprintfn "get query uid %s" uid

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