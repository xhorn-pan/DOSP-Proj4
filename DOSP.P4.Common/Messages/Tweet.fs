// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

open DOSP.P4.Common.Utils

module Tweet =
    open System
    open User
    open HashTag
    open Mention

    type QueryType =
        | QueryUser
        | QueryMention
        | QueryHashtag

    type QueryMsg =
        { QType: QueryType
          Body: string }
        static member QueryByUserId(uid: string) = { QType = QueryUser; Body = uid }
        static member QueryByHashtag(hashtag: string) = { QType = QueryHashtag; Body = hashtag }
        static member QueryByMentionUid(uid: string) = { QType = QueryMention; Body = uid }

    type Tweet =
        { Id: string
          Uid: string
          Text: string
          CreateAt: DateTime
          ReTweet: bool
          RtId: string
          HashTags: HashTag list
          Mentions: Mention list }
