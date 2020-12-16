
# This implementation utilized Akka.Cluster, LibSodium.js, NSec.Cryptography

# Part 2
Using websharper and asp.net.websocket, a Simple web page is implemented to connect
the Akka.net cluster in the part 1. Each websocket endpoint connect to a 
Akka actor. 


## links
[github](https://github.com/xhorn-pan/DOSP-Proj4)
[video](https://youtu.be/bpCuoWTF1M8) 

## code explainations

The websocket server is implemnted in [DOSP.P4.Web/Backend/WebSocketServer.fs](https://github.com/xhorn-pan/DOSP-Proj4/blob/main/DOSP.P4.Web/Backend/WebSocketServer.fs)
The websocket client is implemented in [DOSP.P4.Web/Frontend/WebSocketClient.fs](https://github.com/xhorn-pan/DOSP-Proj4/blob/main/DOSP.P4.Web/Backend/WebSocketServer.fs)

### Akka.net message <=> websocket message

When the actor in the client side(which is also the server side in websharper's view.) 
receive the message from the akka.net cluster, it need transfer the messages to `S2CMessage`.
When it receive from websocket, it need to transfer the `C2SMessage` to the akka message.

                  akka messages           transfer function        websocket message
 Akka cluster server <---> Akka cluster client <---> websokect server <---> websokect client

The transfer function is in the [`wsClientActor`](https://github.com/xhorn-pan/DOSP-Proj4/blob/cc60c3f8abd6ce1bb1c21da0f5944a744ecfc3d7/DOSP.P4.Web/Backend/WebSocketServer.fs#L90) actor. 

The JSON message for the communication of the Server-Client as follow
```fsharp
    [<JavaScript; NamedUnionCases "c2s">]
    type C2SMessage =
        | Request of str: string
        | [<Name "user-reg">] UserReg of pkey: string 
        | [<Name "user-challenge">] UserChalleng of uid: string
        | [<Name "user-login">] UserLogin of uid: string * data: string * signed: string
        | [<Name "user-logout">] UserLogout of uid: string
        | [<Name "user-follow">] UserFollow of uid: string * fid: string
        | [<Name "user-tweet">] UserTweet of uid: string * tweet: string
        | [<Name "user-rt">] UserRt of uid: string * rtid: string * rtuid: string
        | [<Name "query-user">] QTofUser of uid: string
        | [<Name "query-hashtag">] QTofHashTag of hashtag: string
        | [<Name "query-mention">] QTofMention of mention: string

    and [<JavaScript; NamedUnionCases "s2c">] S2CMessage =
        | [<Name "string">] Response of value: string
        | [<Name "tweet">] T of value: string
        | [<Name "er-1">] ERS of msg: string
        | [<Name "er-0">] ERF of msg: string
        | [<Name "user-reg-succ">] URS of uid: string * name: string * pubkey: string
        | [<Name "challenge">] Challenge of uid: string * challenge: string
```

For example, when it receive `QTofUser` 
it send message to query actor, and the latter take it from there.

```fsharp
    | QTofUser uid -> qRef <! QueryMsg.QueryByUserId uid
```

From akka message to websocket, I only transfer `SUser`, `EngineResp`, 
the `Tweet` seems not working, it catched by the wildcard.

## BOUNS credits request
I request the bouns credits for the bouns 1 and 2. I will explain in the follow
section.

### User register for the BOUNS 1
When new user request registeration, the web client generate keys using Edwards-curve 
Digital Signature Algorithm to generate 256 bits public key and 512 bit private key.
The private key is keep in the web browser's local storage.
the key generation function as follow: (use libsodium.js)
```fsharp
    [<Direct """
        var kp = sodium.crypto_sign_keypair();
        return {'skey': sodium.to_hex(kp.publicKey), 'ckey': sodium.to_hex(kp.privateKey)};
    """>]
    let genKeyX25519 () = X(obj)
```
In week report 12, I give the method to use libsodium.js in websharper.

### User login functions for the BOUNS 2
UserChalleng and UserLogin are the message for this part.
the UserChalleng process code as follow, on the server side, it user only fsharp(DotNet) to 
generate a 32 bytes(256 bits) data,
send it to client as hex string.
```fsharp
| UserChalleng uid ->
    let chal = Array.zeroCreate 32
    let rng = new RNGCryptoServiceProvider()
    rng.GetNonZeroBytes chal
    client.PostAsync(Challenge(uid, (chal |> ByteToHex)))
    |> Async.Start
```

When the client got the challenge, the follow function format the challenge with 
timestamp, signed it, and then send the original data and signed data to the server.
(Because on the server side, the NSec.Cryptography library can only verify the detached signature.)
```fsharp
    [<Direct """
        var prikey = sodium.from_hex($key);
        var now = new Date();
        var ts = Math.floor(now.getTime() / 1000);
        var plain = $ch + "." + ts.toString();
        var signed = sodium.crypto_sign_detached(plain, prikey);
        return {'data': sodium.to_hex(plain), 'signed': sodium.to_hex(signed)};
    """>]
    let signCh (key: string) (ch: string) = X(obj)
```

then send data for the server to verify:

```fsharp
let signed = (signCh pk ch) |> Json.Decode<SignStruct>
async { server.Post(WSServer.UserLogin(uid, signed.Data, signed.Signed))} |> Async.Start

```

At last, on the server side, it verify the data, the do next accordingly.
Here is the code
```fsharp

| UserLogin (uid, data, sign) ->
    // user from db
    let hk = user.[0].PubKey |> fromHex
    let alg = SignatureAlgorithm.Ed25519
    let key =
        PublicKey.Import(alg, ReadOnlySpan<byte>(hk), KeyBlobFormat.RawPublicKey)
    let verified =
        alg.Verify(key, ReadOnlySpan<byte>(data |> fromHex), ReadOnlySpan<byte>(sign |> fromHex))
    if verified then
        let dcode =
            data |> fromHex |> Text.Encoding.ASCII.GetString
        let ts = dcode.Split('.').[1] |> int64
        let now = DateTimeOffset.Now
        if (now.ToUnixTimeSeconds() - ts < 1L) then
            let user = SUser.LogIOU uid
            uRef <! UserCmd.LoginUser user
            client.PostAsync(Response "public key verified, logged in")
            |> Async.Start
        else
            client.PostAsync(Response "public key verify timeout, try login again")
            |> Async.Start
    else
        client.PostAsync(Response "public key verify error")
        |> Async.Start
```

