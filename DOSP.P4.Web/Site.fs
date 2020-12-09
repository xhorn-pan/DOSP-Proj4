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

    type EndPoint = | [<EndPoint "/">] Home

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

                )
