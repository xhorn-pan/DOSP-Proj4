namespace DOSP.P4.Common.Messages

module User =
    type User = { Id: int64; Name: string }

    type UserCmdType =
        | Register
        | Login
        | Logout

    type UserCmd = { Cmd: UserCmdType; User: User }
    let CreateUser (id: int64) (name: string) = { Id = id; Name = name }
    let RegisterUser (u: User) = { Cmd = Register; User = u }
    let LoginUser (u: User) = { Cmd = Login; User = u }
    let LogoutUser (u: User) = { Cmd = Logout; User = u }
