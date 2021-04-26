
open System
open System.Net.Http
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Logging
open Server.Configurations
open Server.Handler
open Server.Middleware
open FSharp.Control.Tasks.V2.ContextInsensitive

let address = $"{serviceSettings.Protocol}://localhost:{serviceSettings.Port}"
let health = "/health"
let routes =
    choose [
        routeStartsWith health >=> GET >=> text(DateTime.Now.ToString())
        logRequest >=> choose [
                routeStartsWith "/api/" >=> validateApiKey >=> route "/api/token" >=> choose [
                    POST >=> createTokenHandler
                    RequestErrors.METHOD_NOT_ALLOWED "Method not allowed!"
                ]
                notFoundHandler
            ]
    ]

let runHeartbeatMonitor() = 
    let timer = new System.Timers.Timer(Interval=1000.0,AutoReset=true,Enabled=true);
    timer.Elapsed.Add(
        fun _ ->
            let t = task {
                use restClient = new HttpClient()
                let sendingRequestAt = DateTime.Now
                let! res = restClient.GetAsync($"{address}{health}")
                let! body = res.Content.ReadAsStringAsync()
                let handleAt = DateTime.Parse body
                let offset = sendingRequestAt - handleAt
                let log =
                    StringBuilder("[Status] = ").Append(res.StatusCode).Append(Environment.NewLine)
                        .Append("[Sent] = ").Append(sendingRequestAt.ToString("o")).Append(Environment.NewLine)
                        .Append("[Handled] = ").Append(handleAt.ToString("o")).Append(Environment.NewLine)
                        .Append("[Offset] = ").Append(offset.Milliseconds.ToString()).Append("ms").ToString()
                Server
                    .Logging.loggerProvider
                    .CreateLogger("Heartbeat Monitor")
                    .LogInformation(log)
                return ()
            }
            t.Result
        )

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe routes
        
let configureLogging (builder : ILoggingBuilder) =
    
    builder
        .ClearProviders()
        .AddProvider(Server.Logging.loggerProvider)
        |> ignore

let configureServices (services : IServiceCollection) =
    addSingletonServices services
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main _ =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(
            fun (webHostBuilder: IWebHostBuilder) ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureLogging(configureLogging)
                    .ConfigureServices(configureServices)
                    .UseUrls(address)
                    |> ignore
                runHeartbeatMonitor()
            )
        .Build()
        .Run()
    0