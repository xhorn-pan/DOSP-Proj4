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

    let FollowUserCmd (u: User) (f: User) = FollowUserIdCmd u.Id f.Id

    let UnfollowUserIdCmd (uid: string) (fid: string) =
        { Cmd = Unfollow
          Col = { UserId = uid; FollowerId = fid } }

    let UnfollowUserCmd (u: User) (f: User) = UnfollowUserIdCmd u.Id f.Id
