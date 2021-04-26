module Server.Configurations

open System
open FirebaseAdmin
open FSharp.Data
open FirebaseUtil
open Microsoft.Extensions.DependencyInjection

type ApiKeyValidator = Guid -> bool

type FbApp = FirebaseApp

let private firebaseSettings = JsonProvider<"Resources/Firebase.json">.GetSample()
let serviceSettings = JsonProvider<"Resources/Service.json">.GetSample()

let addSingletonServices (services : IServiceCollection) =
    
    FbApp.createFromFile firebaseSettings.ProjectId firebaseSettings.CredentialsPath 
    |> services.AddSingleton<FbApp>
    |> ignore
     
    serviceSettings.ApiKey.ToString()
    |> Guid
    |> fun apiKey -> apiKey.Equals
    |> services.AddSingleton<ApiKeyValidator>
    |> ignore