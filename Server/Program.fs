
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.Extensions.Logging
open Server.Configurations
open Server.Logging
open Server.Handler
open Server.Middleware

let routes =
    logRequest >=> choose [
            routeStartsWith "/api/" >=> validateApiKey >=> route "/api/token" >=> choose [
                POST >=> createTokenHandler
                RequestErrors.METHOD_NOT_ALLOWED "Method not allowed!"
            ]
            notFoundHandler
        ]

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe routes
        
let configureLogging (builder : ILoggingBuilder) =
    builder
        .ClearProviders()
        .AddProvider(loggerProvider())
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