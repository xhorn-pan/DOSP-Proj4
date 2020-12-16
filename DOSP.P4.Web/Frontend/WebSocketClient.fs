// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Web.Frontend

module WebSocketClient =

    open WebSharper
    open WebSharper.JavaScript
    open WebSharper.UI
    open WebSharper.UI.Html
    open WebSharper.UI.Client
    open WebSharper.AspNetCore.WebSocket
    open WebSharper.AspNetCore.WebSocket.Client
    open WebSharper.Core.Resources

    module WSServer = DOSP.P4.Web.Backend.WebSocketServer

    type Sodium() =
        inherit BaseResource("sodium.js")

    [<assembly:Require(typeof<Sodium>)>]
    do ()

    [<JavaScript>]
    type Keys =
        { [<Name "skey">]
          PubKey: string
          [<Name "ckey">]
          PriKey: string }

    // save and search from local storage
    [<JavaScript>]
    let getLocal (key: string): string option =
        try
            let value = JS.Window.LocalStorage.GetItem key
            Some(value)

        with ex -> None

    [<JavaScript>]
    let saveLocal (uid: string) (pubkey: string) =
        JS.Window.LocalStorage.SetItem(uid, pubkey)

    [<Direct """
        var kp = sodium.crypto_sign_keypair();
        return {'skey': sodium.to_hex(kp.publicKey), 'ckey': sodium.to_hex(kp.privateKey)};
    """>]
    let genKeyX25519 () = X(obj)

    [<Direct "{'_id': '', 'name': $name, 'skey': $key.skey}">]
    let getUserForReg (name: string) key = X(obj)

    [<Direct "{'_id': '', 'name': $name, 'ckey': $key.ckey}">]
    let getUserForClient (name: string) key = X(obj)

    [<JavaScript>]
    type SignStruct =
        { [<Name "data">]
          Data: string
          [<Name "signed">]
          Signed: string }

    [<Direct """
        var prikey = sodium.from_hex($key);
        var now = new Date();
        var ts = Math.floor(now.getTime() / 1000);
        var plain = $ch + "." + ts.toString();
        var signed = sodium.crypto_sign_detached(plain, prikey);
        return {'data': sodium.to_hex(plain), 'signed': sodium.to_hex(signed)};
    """>]
    let signCh (key: string) (ch: string) = X(obj)

    [<JavaScript>]
    let newUserPanel (server: WebSocketServer<WSServer.S2CMessage, WSServer.C2SMessage>) =
        let upContainer = Elt.div [] []


        let hr1 = Elt.hr [] []
        hr1 |> Doc.RunAppend upContainer.Dom |> ignore
        // user login by uid
        let uid = Var.Create "enter your pid"
        let uidInput = Doc.Input [ attr.name "user-id" ] uid
        uidInput
        |> Doc.RunAppend upContainer.Dom
        |> ignore

        let loginButton =
            button [ on.click (fun _ _ ->
                         async { server.Post(WSServer.UserChalleng uid.Value) }
                         |> Async.Start) ] [
                text "Login"
            ]

        loginButton
        |> Doc.RunAppend upContainer.Dom
        |> ignore

        // follow
        // user login by uid
        let fid = Var.Create "uid of your follower"

        let fidInput = Doc.Input [ attr.name "follow-id" ] fid
        fidInput
        |> Doc.RunAppend upContainer.Dom
        |> ignore

        let foButton =
            button [ on.click (fun _ _ ->
                         async { server.Post(WSServer.UserFollow(uid.Value, fid.Value)) }
                         |> Async.Start) ] [
                text "Set Follower"
            ]

        foButton
        |> Doc.RunAppend upContainer.Dom
        |> ignore

        let hr3 = Elt.hr [] []
        hr3 |> Doc.RunAppend upContainer.Dom |> ignore
        // tweet
        let twtext = Var.Create "tweet some"

        let twInput =
            Doc.InputArea [ attr.name "user-tweet" ] twtext

        twInput |> Doc.RunAppend upContainer.Dom |> ignore

        let tweetButton =
            button [ on.click (fun _ _ ->
                         async { server.Post(WSServer.UserTweet(uid.Value, twtext.Value)) }
                         |> Async.Start) ] [
                text "Send Tweet"
            ]

        tweetButton
        |> Doc.RunAppend upContainer.Dom
        |> ignore

        let rtid = Var.Create "rt id"
        let rtuid = Var.Create "rt uid"
        let rtidInput = Doc.Input [ attr.name "rt-id" ] rtid
        let rtuidInput = Doc.Input [ attr.name "rt-uid" ] rtuid

        let rtButton =
            button [ on.click (fun _ _ ->
                         async { server.Post(WSServer.UserRt(uid.Value, rtid.Value, rtuid.Value)) }
                         |> Async.Start) ] [
                text "RT"
            ]

        rtidInput
        |> Doc.RunAppend upContainer.Dom
        |> ignore
        rtuidInput
        |> Doc.RunAppend upContainer.Dom
        |> ignore
        rtButton
        |> Doc.RunAppend upContainer.Dom
        |> ignore
        upContainer

    [<JavaScript>]
    let WebSocketTest (endpoint: WebSocketEndpoint<WSServer.S2CMessage, WSServer.C2SMessage>) =
        let container = Elt.div [ attr.id "main-container" ] []

        let console =
            Elt.pre [ attr.id "console"
                      attr.style "position: fixed; bottom: 50px; width: 60%; height:20%" ] []

        let writen fmt =
            Printf.ksprintf (fun s ->
                JS.Document.CreateTextNode(s + "\n")
                |> console.Dom.AppendChild
                |> ignore) fmt

        async {
            do writen "Console: show server response"
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
                                           | WSServer.Response x -> writen "Response %s (state %i)" x state
                                           | WSServer.URS (uid, name, pubkey) ->
                                               writen "User reg result id: %s, name %s, key: %s " uid name pubkey
                                               let prikey = getLocal pubkey
                                               match prikey with
                                               | Some pk -> saveLocal uid pk
                                               | None ->
                                                   writen "user lost: can not get private key with pubkey: %s" pubkey

                                           | WSServer.Challenge (uid, ch) ->
                                               writen "user %s got challenge %s from server,signing it" uid ch
                                               let prikey = getLocal uid
                                               match prikey with
                                               | Some pk ->
                                                   let signed =
                                                       (signCh pk ch) |> Json.Decode<SignStruct>

                                                   writen "signed msg: %A" signed
                                                   async {
                                                       server.Post(WSServer.UserLogin(uid, signed.Data, signed.Signed))
                                                   }
                                                   |> Async.Start
                                               | None -> writen "user lost: key for user : %s" uid
                                           | WSServer.ERF msg -> writen "Server Error: %s" msg
                                           | WSServer.ERS msg -> writen "Server Success: %s" msg
                                           | WSServer.T t -> writen "tweet: %s" t

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

            let newUserPageButton =
                button [ on.click (fun _ _ ->
                             let nUP = newUserPanel server
                             nUP |> Doc.RunAppend container.Dom |> ignore) ] [
                    text "New User Panel"
                ]

            newUserPageButton
            |> Doc.RunAppend container.Dom
            |> ignore

            let ugButton = // user register
                button [ on.click (fun _ _ ->
                             async {
                                 let keys = genKeyX25519 () |> Json.Decode<Keys>
                                 saveLocal keys.PubKey keys.PriKey
                                 server.Post(WSServer.UserReg keys.PubKey)
                             }
                             |> Async.Start) ] [
                    text "register new user"
                ]

            ugButton |> Doc.RunAppend container.Dom |> ignore

            let hr4 = Elt.hr [] []

            hr4 |> Doc.RunAppend container.Dom |> ignore
            // query uid
            let qutext = Var.Create "query user"

            let quInput =
                Doc.Input [ attr.name "query-user" ] qutext

            quInput |> Doc.RunAppend container.Dom |> ignore

            let quButton =
                button [ on.click (fun _ _ ->
                             async { server.Post(WSServer.QTofUser qutext.Value) }
                             |> Async.Start) ] [
                    text "Search Tweet"
                ]

            quButton |> Doc.RunAppend container.Dom |> ignore

            // query hashtag
            let qhtext = Var.Create "query hashtag"

            let qhInput =
                Doc.Input [ attr.name "query-ht" ] qhtext

            qhInput |> Doc.RunAppend container.Dom |> ignore

            let qhButton =
                button [ on.click (fun _ _ ->
                             async { server.Post(WSServer.QTofHashTag qhtext.Value) }
                             |> Async.Start) ] [
                    text "Search Tweet"
                ]

            qhButton |> Doc.RunAppend container.Dom |> ignore
            // query mention
            let qmtext = Var.Create "query mention"

            let qmInput = Doc.Input [ attr.name "query-m" ] qmtext

            qmInput |> Doc.RunAppend container.Dom |> ignore

            let qmButton =
                button [ on.click (fun _ _ ->
                             async { server.Post(WSServer.QTofMention qmtext.Value) }
                             |> Async.Start) ] [
                    text "Search Tweet"
                ]

            qmButton |> Doc.RunAppend container.Dom |> ignore


            let nUP = newUserPanel server

            nUP |> Doc.RunAppend container.Dom |> ignore
        }
        |> Async.Start
        // console |> Doc.RunAppend container.Dom |> ignore
        Elt.div [] [ container; console ]

    let MyEndPoint (url: string)
                   : WebSharper.AspNetCore.WebSocket.WebSocketEndpoint<WSServer.S2CMessage, WSServer.C2SMessage> =
        WebSocketEndpoint.Create(url, "/ws", JsonEncoding.Readable)
