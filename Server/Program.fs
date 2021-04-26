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

let private address = $"{serviceSettings.Protocol}://localhost:{serviceSettings.Port}"
let private health = "/api/health"
let private healthAddress = $"{address}{health}"

let routes =
    choose [
        logRequest >=> validateApiKey >=> choose [
                route health >=> GET >=> setStatusCode StatusCodes.Status200OK
                routeStartsWith "/api/" >=> route "/api/token" >=> choose [
                    POST >=> createTokenHandler
                    RequestErrors.METHOD_NOT_ALLOWED "Method not allowed!"
                ] 
                notFoundHandler
        ]
    ]



let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe routes
        
let configureLogging (builder : ILoggingBuilder) =
    builder
        .ClearProviders()
        .AddProvider(Server.Logging.loggerProvider)
        |> ignore

let configureServices (services : IServiceCollection) =
    Server.Logging.loggerProvider |> initServices services healthAddress
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
            )
        .Build()
        .Run()
    0