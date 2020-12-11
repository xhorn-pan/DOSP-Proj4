namespace DOSP.P4.Web.Frontend

module RESTClient =
    open WebSharper
    // from https://kimsereylam.com/fsharp/2015/08/26/single-page-application-in-fsharp-websharper.html
    [<JavaScript>]
    module Async =
        let map f xAsync =
            async {
                let! x = xAsync
                return f x
            }

        let retn x = async { return x }

    [<JavaScript>]
    module AsyncApi =
        type ApiResult<'a> =
            | Success of 'a
            | Failure of ApiResponseException list

        and ApiResponseException =
            | Unauthorized of string
            | NotFound of string
            | BadRequest of string
            | JsonDeserializeError of string

            override this.ToString() =
                match this with
                | ApiResponseException.Unauthorized err -> err
                | ApiResponseException.NotFound err -> err
                | ApiResponseException.BadRequest err -> err
                | ApiResponseException.JsonDeserializeError err -> err

        let map f xAsyncApiResult =
            async {
                let! xApiResult = xAsyncApiResult

                match xApiResult with
                | Success x -> return Success(f x)
                | Failure err -> return Failure err
            }

        let retn x = async { return ApiResult.Success x }

        let apply fAsyncApiResult xAsyncApiResult =
            async {
                let! fApiResult = fAsyncApiResult
                let! xApiResult = xAsyncApiResult

                match fApiResult, xApiResult with
                | Success f, Success x -> return Success(f x)
                | Success f, Failure err -> return Failure err
                | Failure err, Success x -> return Failure err
                | Failure errf, Failure errx -> return Failure(List.concat [ errf; errx ])
            }

        let bind f xAsyncApiResult =
            async {
                let! xApiResult = xAsyncApiResult

                match xApiResult with
                | Success x -> return! f x
                | Failure err -> return (Failure err)
            }

        let start xAsyncApiRes =
            xAsyncApiRes
            |> Async.map (fun x -> ())
            |> Async.Start

        type ApiCallBuilder() =
            member this.Bind(x, f) =
                async {
                    let! xApiResult = x

                    match xApiResult with
                    | Success x -> return! f x
                    | Failure err -> return (Failure err)
                }

            member this.Return x = async { return ApiResult.Success x }
            member this.ReturnFrom x = x

        let apiCall = ApiCallBuilder()

    [<JavaScript>]
    module ApiClient =
        open WebSharper.JavaScript
        open WebSharper.JQuery
        open AsyncApi

        type RequestSettings =
            { RequestType: JQuery.RequestType
              Url: string
              ContentType: string option
              Headers: (string * string) list option
              Data: string option }
            member this.ToAjaxSettings ok ko =
                let settings =
                    JQuery.AjaxSettings
                        (Url = "http://localhost:5000/api/" + this.Url,
                         Type = this.RequestType,
                         DataType = JQuery.DataType.Json,
                         Success = (fun result _ _ -> ok (result)),
                         Error = (fun jqXHR _ _ -> ko (System.Exception(string jqXHR.Status))))

                this.Headers
                |> Option.iter (fun h -> settings?headers <- Object<string>(h |> Array.ofList))
                this.ContentType
                |> Option.iter (fun c -> settings?contentType <- c)
                this.Data
                |> Option.iter (fun d -> settings?data <- d)
                settings

        type User =
            { [<Name "_id">]
              Id: string
              [<Name "name">]
              Name: string
              [<Name "skey">]
              PubKey: string }

        type Api =
            { GetUsers: unit -> Async<ApiResult<User list>>
              RegUser: User -> Async<ApiResult<string>> }
        // GetUser: string -> Async<ApiResult<User>>
        // Login: (string * string) -> Async<ApiResult<unit>> }

        let private ajaxCall (requestSettings: RequestSettings) =
            Async.FromContinuations
            <| fun (ok, ko, _) ->
                requestSettings.ToAjaxSettings ok ko
                |> JQuery.Ajax
                |> ignore

        let private matchErrorStatusCode url code =
            match code with
            | "401" ->
                Failure [ ApiResponseException.Unauthorized
                          <| sprintf " '%s' - 401 The Authorization header did not pass security " url ]
            | "404" ->
                Failure [ ApiResponseException.NotFound
                          <| sprintf " '%s' - 404 Endpoint not found " url ]
            | code ->
                Failure [ ApiResponseException.BadRequest
                          <| sprintf " '%s' - %s Bad request" url code ]

        let private tryDeserialize deserialization input =
            try
                deserialization input |> ApiResult.Success
            with _ ->
                Failure [ ApiResponseException.JsonDeserializeError
                          <| sprintf """"{%s}" cannot be deserialized""" input ]
            |> Async.retn

        let private tryDeserializeList deserialization (input: 'a list) =
            try
                input
                |> List.map (fun i -> (deserialization i))
                |> ApiResult.Success
            with _ ->
                Failure [ ApiResponseException.JsonDeserializeError
                          <| sprintf """"{%A}" cannot be deserialized""" input ]
            |> Async.retn


        let private getToken (priKey: string) =
            try
                JS.Window.LocalStorage.GetItem priKey
                |> ApiResult.Success

            with ex -> ApiResult.Failure [ Unauthorized "local user not found" ]
            |> Async.retn

        let private getUsers () =
            async {
                let url = "user"
                try
                    let! users =
                        ajaxCall
                            { RequestType = JQuery.RequestType.GET
                              Url = url
                              ContentType = None
                              Headers = None
                              Data = None }

                    let ulist: User list = users |> unbox

                    return ApiResult.Success ulist

                with ex -> return matchErrorStatusCode url ex.Message
            }
        //|> AsyncApi.bind (tryDeserialize Json.Deserialize<User list>)

        let private regUser (u: User) =
            async {
                let url = "user"

                try
                    let! uid =
                        ajaxCall
                            { RequestType = JQuery.RequestType.POST
                              Url = url
                              ContentType = Some("application/json")
                              Headers = None
                              Data = Some(u |> Json.Serialize) }

                    return ApiResult.Success(uid.ToString())
                with ex -> return matchErrorStatusCode url ex.Message
            }
        //|> AsyncApi.bind (tryDeserialize Json.Deserialize<string>)

        let api =
            { GetUsers = fun () -> apiCall { return! getUsers () }
              RegUser = fun (u: User) -> apiCall { return! regUser u } }
