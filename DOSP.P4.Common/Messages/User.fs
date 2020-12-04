// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module User =
    open MongoDB.Bson

    type User = { Id: string; Name: string }

    type UserCmdType =
        | Register
        | Login
        | Logout

    type UserCmd = { Cmd: UserCmdType; User: User }

    let CreateUser (name: string) =
        let id = BsonObjectId(ObjectId.GenerateNewId())
        { Id = id.ToString(); Name = name }

    let RegisterUser (u: User) = { Cmd = Register; User = u }
    let LoginUser (u: User) = { Cmd = Login; User = u }
    let LogoutUser (u: User) = { Cmd = Logout; User = u }
