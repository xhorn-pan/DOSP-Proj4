// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module User =
    open MongoDB.Bson
    open WebSharper
    open FSharp.Core

    type UserKey =
        { [<Name "skey">]
          SKey: string
          [<Name "ckey">]
          CKey: string }

    type UserNameWithId =
        { [<Name "_id">]
          Id: string option
          [<Name "name">]
          Name: string }

    // for client, hold the private key
    [<JavaScript>]
    type WSClientUser =
        { [<Name "user">]
          User: UserNameWithId
          [<Name "ckey">]
          CKey: string } // hex

    // for server register, send the public key
    [<JavaScript>]
    type WSServerUser =
        { [<Name "user">]
          User: UserNameWithId
          [<Name "skey">]
          SKey: string } // hex

    [<JavaScript>]
    type User =
        { [<Name "user">]
          User: UserNameWithId
          [<Name "keys">]
          Keys: UserKey }

    // [<Stub>]
    // member this.User4Server(): WSServerUser =
    //     { User = this.User
    //       Key = this.Keys.PubKey }

    // [<Stub>]
    // member this.User4Client(): WSClientUser =
    //     { User = this.User
    //       Key = this.Keys.PriKey }

    [<JavaScript; NamedUnionCases "user_cmd_type">]
    type UserCmdType =
        | [<Constant "register">] Register
        | [<Constant "login">] Login
        | [<Constant "logout">] Logout

    [<JavaScript>]
    type UserCmd = { Cmd: UserCmdType; User: User }

    [<Name "create_user">]
    let CreateUser (name: string) =
        let id = "_id_" + name
        { Id = Some(id.ToString())
          Name = name }

    [<JavaScript; Name "register_user">]
    let RegisterUser (u: User) = { Cmd = Register; User = u }

    [<JavaScript; Name "login_user">]
    let LoginUser (u: User) = { Cmd = Login; User = u }

    [<JavaScript; Name "logout_user">]
    let LogoutUser (u: User) = { Cmd = Logout; User = u }
