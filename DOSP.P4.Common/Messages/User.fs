// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module User =
    open MongoDB.Bson
    open System
    open WebSharper

    let usernameGenerator () =
        let nameLen = [| 4 .. 9 |]

        let r = Random()

        let chars =
            Array.concat
                ([ [| 'a' .. 'z' |]
                   [| 'A' .. 'Z' |]
                   [| '0' .. '9' |] ])

        String(Array.init nameLen.[r.Next nameLen.Length] (fun _ -> chars.[r.Next chars.Length]))

    type SUser =
        { Id: string
          Name: string
          PubKey: string }
        static member Create(pKey: string) =
            let id =
                BsonObjectId(ObjectId.GenerateNewId()).ToString()

            let name = usernameGenerator ()
            { Id = id; Name = name; PubKey = pKey }

        static member LogIOU(id: string) = { Id = id; Name = ""; PubKey = "" }

    type UserCmdType =
        | Register
        | Login
        | Logout

    type UserCmd =
        { Cmd: UserCmdType
          User: SUser }
        static member RegisterUser(u: SUser) = { Cmd = Register; User = u }
        static member LoginUser(u: SUser) = { Cmd = Login; User = u }
        static member LogoutUser(u: SUser) = { Cmd = Logout; User = u }
