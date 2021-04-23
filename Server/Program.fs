
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Logging
open Server.Configurations
open Server.Logging

let webApp =
    Server.Handler.logRequest >=> choose [
        POST >=> routeStartsWith "/api/" >=> choose [
        Server.Handler.validateApiKey >=> route "/api/token" >=> Server.Handler.handler
        Server.Handler.notFoundHandler] 
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe webApp
        
let configureLogging (builder : ILoggingBuilder) =
    builder
        .ClearProviders()
        .AddProvider(new FileLoggerProvider())
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
                    .UseUrls($"{serviceSettings.Protocol}://localhost:{serviceSettings.Port}")
                    |> ignore)
        .Build()
        .Run()
    0