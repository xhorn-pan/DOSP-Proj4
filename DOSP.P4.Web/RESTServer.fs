namespace DOSP.P4.Web

open WebSharper
open WebSharper.Sitelets
open Microsoft.Extensions.Logging

module RESTServer =
    module Model =

        type User =
            { [<Name "_id">]
              Id: string
              [<Name "name">]
              Name: string
              [<Name "skey">]
              PubKey: string }


        type APIEndPoint =
            | [<EndPoint "GET /user">] GetUsers
            | [<EndPoint "GET /user">] GetUser of Id: string
            | [<EndPoint "POST /user"; Json "user">] CreateUser of user: User
            | [<EndPoint "POST /user">] PostUserTest

        type Error = { Error: string }
        type APIResult<'T> = Result<'T, Http.Status * Error>

    module Backend =
        open Model
        open MongoDB.Driver
        open MongoDB.Bson
        open MongoDB.FSharp
        open DOSP.P4.Common.Utils

        let UserNotFound (): APIResult<'T> =
            Error(Http.Status.NotFound, { Error = "user not found" })

        let GetUsers (logger: ILogger<_>): APIResult<User []> =
            logger.LogInformation("Getting users")
            let uDb = P4GetCollection<User> "user"

            let users =
                uDb.FindSync(Builders.Filter.Empty).ToList()

            users |> Array.ofSeq |> Ok

        let GetUser (logger: ILogger<_>) (id: string): APIResult<User> =
            let uDb = P4GetCollection<User> "user"
            logger.LogInformation("Getting users by id %s", id)

            let users =
                uDb.FindAsync(fun x -> x.Id = id).GetAwaiter().GetResult()

            try
                let user = users.ToEnumerable() |> Seq.exactlyOne
                user |> Ok
            with _ -> UserNotFound()

        let PostUserTest (logger: ILogger<_>): APIResult<string> =
            logger.LogInformation("post test")

            "no problem" |> Ok

        let CreateUser (logger: ILogger<_>) (u: User): APIResult<string> =
            logger.LogInformation("insert %A", u)

            let user =
                if u.Id = "" then
                    { u with
                          Id = BsonObjectId(ObjectId.GenerateNewId()).ToString() }
                else
                    u


            let uDb = P4GetCollection<User> "user"
            try
                uDb.InsertOneAsync(user).GetAwaiter().GetResult()
                user.Id |> Ok
            with _ -> Error(Http.Status.Forbidden, { Error = "user already registered" })
