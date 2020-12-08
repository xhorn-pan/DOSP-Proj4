module DOSP.P4.Web.WebSocketServer

open WebSharper
open WebSharper.AspNetCore.WebSocket.Server
open Akka.Actor
open Akka.FSharp
open DOSP.P4.Common.Utils

// TODO add akka.net
[<JavaScript; NamedUnionCases>]
type C2SMessage =
    | Request1 of str: string []
    | Request2 of int: int []

and [<JavaScript; NamedUnionCases "type">] S2CMessage =
    | [<Name "int">] Response2 of value: int
    | [<Name "string">] Response1 of value: string


let config = ConfigurationLoader.load ()
let system = ActorSystem.Create("project4", config)

let wsConnector (client: WebSocketClient<S2CMessage, C2SMessage>) =
    try
        let wsClientActor (client: WebSocketClient<S2CMessage, C2SMessage>) (mailbox: Actor<C2SMessage>) =
            let rec loop sender =
                actor {
                    let! untypeMessage = mailbox.Receive()

                    match untypeMessage with
                    | Request1 s ->
                        logErrorf mailbox "get msg: %A" s
                        client.PostAsync(Response1 s.[0]) |> Async.Start
                    | Request2 i ->
                        logErrorf mailbox "get msg: %A" i
                        client.PostAsync(Response2 i.[0]) |> Async.Start

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
                               do! client.PostAsync(Response1("Error: " + exn.Message))
                               return state
                           | Close ->
                               dprintfn "Closed connection to %s" clientIp
                               return state
                       })
        }
