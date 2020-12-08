// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module User =
    open MongoDB.Bson
    open WebSharper

    [<JavaScript>]
    type User =
        { [<Name "_id">]
          Id: string
          [<Name "name">]
          Name: string }

    [<JavaScript; NamedUnionCases "user_cmd_type">]
    type UserCmdType =
        | [<Constant "register">] Register
        | [<Constant "login">] Login
        | [<Constant "logout">] Logout

    [<JavaScript>]
    type UserCmd = { Cmd: UserCmdType; User: User }

    [<JavaScript; Name "create_user">]
    let CreateUser (name: string) =
        let id = BsonObjectId(ObjectId.GenerateNewId())
        { Id = id.ToString(); Name = name }

    [<JavaScript; Name "register_user">]
    let RegisterUser (u: User) = { Cmd = Register; User = u }

    [<JavaScript; Name "login_user">]
    let LoginUser (u: User) = { Cmd = Login; User = u }

    [<JavaScript; Name "logout_user">]
    let LogoutUser (u: User) = { Cmd = Logout; User = u }
