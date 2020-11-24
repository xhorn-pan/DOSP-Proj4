// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Common

open System

module Messages =
    open DOSP.P4.Common.Utils
    open Akka.Actor
    open Akka.DistributedData

    //[<Serializable>]
    type HashTag = { Text: string; Indices: int * int }

    let GetHashTags (text: string) =
        let tags = extractText text '#'
        tags
        |> List.map (fun (txt, se) -> { Text = txt; Indices = se })

    //[<Serializable>]
    type User = { Id: int64; Name: string }

    let CreateUser (id: int64) (name: string) = { Id = id; Name = name }

    type UserCmdType =
        | Register
        | Login
        | Logout

    type UserCmd = { Cmd: UserCmdType; User: User }

    let RegisterUser (u: User) = { Cmd = UserCmdType.Register; User = u }
    let LoginUser (u: User) = { Cmd = UserCmdType.Login; User = u }
    let LogoutUser (u: User) = { Cmd = UserCmdType.Logout; User = u }
