
open System
open System.Diagnostics
open System.Net.Http
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
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
        routeStartsWith health >=> GET >=> setStatusCode StatusCodes.Status200OK
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
            use restClient = new HttpClient()
            let st = Stopwatch()
            let res = restClient.GetAsync($"{address}{health}").Result
            let log =
                StringBuilder("[Status] = ").Append(res.StatusCode).Append(Environment.NewLine)
                    .Append("[TicksOffset] = ").Append(st.ElapsedTicks).Append(" ticks").ToString()
            Server
                .Logging.loggerProvider
                .CreateLogger("Heartbeat Monitor")
                .LogInformation(log)
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