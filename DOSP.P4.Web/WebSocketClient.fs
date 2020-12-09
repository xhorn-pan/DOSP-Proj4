module DOSP.P4.Web.WebSocketClient

open WebSharper
open WebSharper.JavaScript
open WebSharper.Json
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Client

open WebSharper.Core.Resources

module Server = WebSocketServer

type Sodium() =
    inherit BaseResource("sodium.js")

[<assembly:Require(typeof<Sodium>)>]
do ()

[<Direct """
    var kp = sodium.crypto_kx_keypair();
    return {'pub': sodium.to_hex(kp.publicKey), 'pri': sodium.to_hex(kp.privateKey)};
""">]
let genKeyX25519 () = X(obj)

[<JavaScript>]
type WSClientUser =
    { [<Name "name">]
      Name: string
      [<Name "pri-key">]
      Key: string } // hex

[<JavaScript>]
let WebSocketTest (endpoint: WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage>) =

    let console = Elt.pre [] []

    let writen fmt =
        Printf.ksprintf (fun s ->
            JS.Document.CreateTextNode(s + "\n")
            |> console.Dom.AppendChild
            |> ignore) fmt

    async {
        do writen "I DON'T KNOW WHAT I AM DOING"
        let! server =
            ConnectStateful endpoint
            <| fun server ->
                async {
                    return 0,
                           (fun state msg ->
                               async {
                                   match msg with
                                   | Message data ->
                                       match data with
                                       | Server.Response x -> writen "Response %s (state %i)" x state

                                       return (state + 1)
                                   | Close ->
                                       writen "WebSocket connection closed."
                                       return state
                                   | Open ->
                                       writen "WebSocket connection open."
                                       return state
                                   | Error ->
                                       writen "WebSocket connection error!"
                                       return state
                               })
                }

        let lotsOfHellos =
            genKeyX25519 ()
            |> Json.Stringify
            |> Array.create 2

        while true do
            do! Async.Sleep 1000
            server.Post(Server.Request lotsOfHellos)

    }
    |> Async.Start
    let container = Elt.div [] [ console ]
    container

let MyEndPoint (url: string): WebSharper.AspNetCore.WebSocket.WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage> =
    WebSocketEndpoint.Create(url, "/ws", JsonEncoding.Readable)
