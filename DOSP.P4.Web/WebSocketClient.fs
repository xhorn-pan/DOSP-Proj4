module DOSP.P4.Web.WebSocketClient

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Client

module Server = WebSocketServer

[<JavaScript>]
let WebSocketTest (endpoint: WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage>) =
    let container = Elt.pre [] []

    let writen fmt =
        Printf.ksprintf (fun s ->
            JS.Document.CreateTextNode(s + "\n")
            |> container.Dom.AppendChild
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
                                       | Server.Response1 x -> writen "Response1 %s (state %i)" x state
                                       | Server.Response2 x -> writen "Response2 %i (state %i)" x state

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

        let lotsOfHellos = "Hello" |> Array.create 1000

        let lotsOf123s = 123 |> Array.create 1000

        while true do
            do! Async.Sleep 1000
            server.Post(Server.Request1 [| "Hello" |])
            do! Async.Sleep 1000
            server.Post(Server.Request2 lotsOf123s)

    }
    |> Async.Start
    container

let MyEndPoint (url: string): WebSharper.AspNetCore.WebSocket.WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage> =
    WebSocketEndpoint.Create(url, "/ws", JsonEncoding.Readable)
