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

    let FollowUserIdCmd (uid: string) (fid: string) =
        { Cmd = Follow
          Col = { UserId = uid; FollowerId = fid } }

    let FollowUserCmd (u: SUser) (f: SUser) = FollowUserIdCmd u.Id f.Id

    let UnfollowUserIdCmd (uid: string) (fid: string) =
        { Cmd = Unfollow
          Col = { UserId = uid; FollowerId = fid } }

    let UnfollowUserCmd (u: SUser) (f: SUser) = UnfollowUserIdCmd u.Id f.Id
