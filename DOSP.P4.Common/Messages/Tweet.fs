namespace DOSP.P4.Common.Messages

open DOSP.P4.Common.Utils

module Tweet =
    open User
    open HashTag
    open Mention

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