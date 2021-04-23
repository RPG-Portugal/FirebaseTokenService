module Server.Configurations

open System
open FirebaseAdmin
open FSharp.Data
open FirebaseAdminUtil
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

type ApiKeyValidator = Guid -> bool

type FbApp = FirebaseApp

let private settings = JsonProvider<"Resources/Service.json">.GetSample()

let addSingletonServices (services : IServiceCollection) =
    
    FbApp.createFromFile settings.ProjectId settings.CredentialsPath 
    |> services.AddSingleton<FbApp>
    |> ignore
     
    settings.ApiKey.ToString()
    |> Guid
    |> fun apiKey -> apiKey.Equals
    |> services.AddSingleton<ApiKeyValidator>
    |> ignore