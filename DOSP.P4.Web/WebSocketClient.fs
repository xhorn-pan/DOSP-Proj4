module DOSP.P4.Web.WebSocketClient

open WebSharper
open WebSharper.JavaScript
open WebSharper.Json
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Client

open WebSharper.Core.Resources
open DOSP.P4.Web.RESTClient
open DOSP.P4.Web.RESTClient.ApiClient

module Server = WebSocketServer

type Sodium() =
    inherit BaseResource("sodium.js")

[<assembly:Require(typeof<Sodium>)>]
do ()

[<JavaScript>]
type CUser =
    { [<Name "_id">]
      Id: string
      [<Name "name">]
      Name: string
      [<Name "ckey">]
      PriKey: string }

[<Direct """
    var kp = sodium.crypto_kx_keypair();
    return {'skey': sodium.to_hex(kp.publicKey), 'ckey': sodium.to_hex(kp.privateKey)};
""">]
let genKeyX25519 () = X(obj)

[<Direct "{'_id': '', 'name': $name, 'skey': $key.skey}">]
let getUserForReg (name: string) key = X(obj)

[<Direct "{'_id': '', 'name': $name, 'ckey': $key.ckey}">]
let getUserForClient (name: string) key = X(obj)

[<JavaScript>]
let regNewUser (userName: string) =
    let key = genKeyX25519 () //|> Json.Decode<UserKey>

    let su: User = getUserForReg userName key
    let cu: CUser = getUserForClient userName key
    (su, cu)

[<JavaScript>]
let WebSocketTest (endpoint: WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage>) =
    let container = Elt.div [] []
    let console = Elt.pre [] []
    console |> Doc.RunAppend container.Dom |> ignore

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


        let nuButton =
            button [ attr.id "new-user"
                     on.click (fun _ _ ->
                         async {
                             let (su, cu) = regNewUser "test"

                             let! uid = api.RegUser su

                             match uid with
                             | AsyncApi.Failure err -> writen "reg user err %A" err
                             | AsyncApi.Success id -> JS.Window.LocalStorage.SetItem(id, cu.PriKey)

                         }
                         |> AsyncApi.start) ] [
                text "new User"
            ]

        let uButton =
            button [ attr.id "get-users"
                     on.click (fun _ _ ->
                         async {
                             let! resp = api.GetUsers()

                             match resp with
                             | AsyncApi.Failure err -> writen "get user err %A" err
                             | _ -> ()

                             return resp
                         }
                         |> Async.map (fun u ->
                             match u with
                             | AsyncApi.Success user ->
                                 user
                                 |> Seq.iter (fun usr -> writen "get user %A" usr)
                             | _ -> ())
                         |> Async.Start) ] [
                text "get Users"
            ]

        nuButton |> Doc.RunAppend container.Dom |> ignore

        uButton |> Doc.RunAppend container.Dom |> ignore
    // while true do
    //     do! Async.Sleep 1000
    //     server.Post(Server.Request lotsOfHellos)

    }
    |> Async.Start

    container

let MyEndPoint (url: string): WebSharper.AspNetCore.WebSocket.WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage> =
    WebSocketEndpoint.Create(url, "/ws", JsonEncoding.Readable)
