namespace DOSP.P4.Common.Messages

module Follow =
    open User

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
