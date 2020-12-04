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

    type QueryMsg = { QType: QueryType; Body: string }

    type Tweet =
        { Id: string
          User: User
          Text: string
          CreateAt: DateTime
          ReTweet: bool
          RtId: string
          HashTags: HashTag list
          Mentions: Mention list }
