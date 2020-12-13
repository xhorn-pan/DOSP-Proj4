namespace DOSP.P4.Web.Backend

open WebSharper
open WebSharper.Sitelets
open Microsoft.Extensions.Logging

module RESTServer =
    module Model =
        open DOSP.P4.Common.Messages.User
        open DOSP.P4.Common.Messages.Follow

        type APIEndPoint =
            | [<EndPoint "GET /user">] GetUsers
            | [<EndPoint "GET /user">] GetUser of Id: string
            | [<EndPoint "POST /user"; Json "user">] CreateUser of user: SUser
            | [<EndPoint "POST /user">] PostUserTest
            | [<EndPoint "POST /follow"; Json "fc">] FollowShip of fc: FollowCollection
            | [<EndPoint "GET /q"; Query("uid", "hashtag", "mention")>] TwQury of uid: string option * hashtag: string option * mention: string option

        type Error = { Error: string }
        type APIResult<'T> = Result<'T, Http.Status * Error>

    module Backend =
        open Model
        open MongoDB.FSharp
        open MongoDB.Driver
        open MongoDB.Bson
        open DOSP.P4.Common.Utils
        open DOSP.P4.Common.Messages.User
        open DOSP.P4.Common.Messages.Follow

        let NotFound (msg: string): APIResult<'T> =
            Error(Http.Status.NotFound, { Error = sprintf "%s not found" msg })

        let FollowShip (logger: ILogger<_>) (fc: FollowCollection): APIResult<string> =
            let db =
                DB.P4GetCollection<FollowCollection> "follow"

            try
                db.InsertOneAsync(fc).GetAwaiter().GetResult()
                Ok("follow done")
            with _ -> NotFound("follow id")

        let TwQury (logger: ILogger<_>) (uid: string option, hashtag: string option, mention: string option) =
            NotFound("not impl")

        let GetUsers (logger: ILogger<_>): APIResult<SUser []> =
            logger.LogInformation("Getting users")
            let uDb = DB.P4GetCollection<SUser> "user"

            let users =
                uDb.FindSync(Builders.Filter.Empty).ToList()

            users |> Array.ofSeq |> Ok

        let GetUser (logger: ILogger<_>) (id: string): APIResult<SUser> =
            let uDb = DB.P4GetCollection<SUser> "user"
            logger.LogInformation("Getting users by id %s", id)

            let users =
                uDb.FindAsync(fun x -> x.Id = id).GetAwaiter().GetResult()

            try
                let user = users.ToEnumerable() |> Seq.exactlyOne
                user |> Ok
            with _ -> NotFound("user")

        let PostUserTest (logger: ILogger<_>): APIResult<string> =
            logger.LogInformation("post test")

            "no problem" |> Ok

        let CreateUser (logger: ILogger<_>) (u: SUser): APIResult<string> =
            logger.LogInformation("insert %A", u)

            let user =
                if u.Id = "" then
                    { u with
                          Id = BsonObjectId(ObjectId.GenerateNewId()).ToString() }
                else
                    u


            let uDb = DB.P4GetCollection<SUser> "user"
            try
                uDb.InsertOneAsync(user).GetAwaiter().GetResult()
                user.Id |> Ok
            with _ -> Error(Http.Status.Forbidden, { Error = "user already registered" })
