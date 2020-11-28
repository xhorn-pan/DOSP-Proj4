// Name: Xinghua Pan
// UFID: 95160902
namespace DOSP.P4.Common

open System

module Messages =
    open DOSP.P4.Common.Utils

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

    type FollowType =
        | Follow
        | Unfollow

    type FollowCmd =
        { Cmd: FollowType
          UserId: int64
          FollowId: int64 }

    let FollowUserCmd (u: User) (f: User) =
        { Cmd = Follow
          UserId = u.Id
          FollowId = f.Id }

    let UnfollowUserCmd (u: User) (f: User) =
        { Cmd = Unfollow
          UserId = u.Id
          FollowId = f.Id }

    //[<Serializable>]
    type HashTag = { Text: string; Indices: int * int }

    let GetHashTags (text: string) =
        let tags = extractText text '#'
        tags
        |> List.map (fun (txt, se) -> { Text = txt; Indices = se })

    type Mention = { User: User; Indices: int * int }

    let GetMentions (text: string) =
        let ms = extractText text '@'
        ms
        |> List.map (fun (txt, se) ->
            let user = CreateUser 0L txt
            { User = user; Indices = se })

    type TweetType =
        | NewT
        | RT
        | Publish

    type TweetCmd =
        { TwType: TweetType
          User: User
          Msg: string }

    let TweetTweet (u: User) (msg: string) = { TwType = NewT; User = u; Msg = msg }

    let PubTweet (tc: TweetCmd) =
        { TwType = Publish
          User = tc.User
          Msg = tc.Msg }

    let RtTweet (u: User) (tc: TweetCmd) =
        let rtMsg = "@" + tc.User.Name + " " + tc.Msg
        { TwType = RT; User = u; Msg = rtMsg }

    type Tweet =
        { Tid: int64
          User: User
          Text: string
          TwType: TweetType
          RtId: int64
          HashTags: HashTag list
          Mentions: Mention list }

    let PubTweetWithId (id: int64) (u: User) (text: string) =
        let hts = GetHashTags text
        let ms = GetMentions text
        { Tid = id
          User = u
          Text = text
          TwType = NewT
          RtId = 0L
          HashTags = hts
          Mentions = ms }
