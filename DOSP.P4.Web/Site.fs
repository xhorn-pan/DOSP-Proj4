namespace DOSP.P4.Web

module Website =

    open WebSharper
    open WebSharper.AspNetCore
    open WebSharper.JavaScript
    open WebSharper.Sitelets
    open WebSharper.UI
    open WebSharper.UI.Html
    open WebSharper.UI.Templating
    open WebSharper.UI.Server
    open Microsoft.Extensions.Logging

    open RESTServer


    type EndPoint =
        | [<EndPoint "/">] Home
        | [<EndPoint "/api">] API of Cors<Model.APIEndPoint>

    let JsonAPI (result: Model.APIResult<'T>): Async<Content<EndPoint>> =
        match result with
        | Ok value -> Content.Json value
        | Error (status, error) -> Content.Json error |> Content.SetStatus status
        |> Content.WithContentType "application/json"

    let APIContent logger (ep: Model.APIEndPoint): Async<Content<EndPoint>> =
        match ep with
        | Model.GetUsers -> JsonAPI(Backend.GetUsers logger)
        | Model.GetUser id -> JsonAPI(Backend.GetUser logger id)
        | Model.CreateUser user -> JsonAPI(Backend.CreateUser logger user)
        | Model.PostUserTest -> JsonAPI(Backend.PostUserTest logger)

    type MainTemplate = Templating.Template<"Main.html", clientLoad=ClientLoad.FromDocument>

    [<JavaScript>]
    module Client =
        let Main wsep =
            MainTemplate.Body().WebSocketTest(WebSocketClient.WebSocketTest wsep).Doc()

    type MyWebSite(logger: ILogger<MyWebSite>) =
        inherit SiteletService<EndPoint>()

        override this.Sitelet =
            Application.MultiPage(fun (ctx: Context<_>) endpoint ->
                match endpoint with
                | EndPoint.Home ->
                    let wsep =
                        WebSocketClient.MyEndPoint(ctx.RequestUri.ToString())

                    MainTemplate().Main(client <@ Client.Main wsep @>).Doc()
                    |> Content.Page
                | EndPoint.API endpoint ->
                    Content.Cors endpoint (fun allows ->
                        { allows with
                              Origins = [ "http://localhost:5000" ]
                              Headers = [ "Content-Type" ] }) (APIContent <| logger))
