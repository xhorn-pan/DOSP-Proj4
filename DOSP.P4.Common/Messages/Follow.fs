// Name: Xinghua Pan
// UFID: 95160902

namespace DOSP.P4.Common.Messages

module Follow =
    open User

    type FollowType =
        | Follow
        | Unfollow

    // [ BsonIgnoreExtraElementsAttribute(true) ]
    type FollowCollection = { UserId: string; FollowerId: string }

    type FollowCmd =
        { Cmd: FollowType
          Col: FollowCollection }
        static member FollowUserIdCmd (uid: string) (fid: string) =
            { Cmd = Follow
              Col = { UserId = uid; FollowerId = fid } }

        static member FollowUserCmd (u: SUser) (f: SUser) = FollowCmd.FollowUserIdCmd u.Id f.Id

        static member UnfollowUserIdCmd (uid: string) (fid: string) =
            { Cmd = Unfollow
              Col = { UserId = uid; FollowerId = fid } }

        static member UnfollowUserCmd (u: SUser) (f: SUser) = FollowCmd.UnfollowUserIdCmd u.Id f.Id
