module Server.Configurations

open System
open System.Diagnostics
open System.Net.Http
open System.Text
open FirebaseAdmin
open FSharp.Data
open FirebaseUtil
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

type ApiKeyValidator = Guid -> bool

type FbApp = FirebaseApp

let private firebaseSettings = JsonProvider<"Resources/Firebase.json">.GetSample()
let serviceSettings = JsonProvider<"Resources/Service.json">.GetSample()

let startHeartbeatMonitor (apiKey: Guid) (address: string) (loggerProvider: ILoggerProvider) =
        let logger = loggerProvider.CreateLogger("Heartbeat Monitor")    
        let timer = new System.Timers.Timer(Interval=1000.0,AutoReset=true,Enabled=true)
        timer.Elapsed.Add(
            fun _ ->
                use restClient = new HttpClient()
                let st = Stopwatch()
                use httpRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, address)
                httpRequest.Headers.Add("X-API-Key", apiKey.ToString())
                let res = restClient.Send(httpRequest)
                let log =
                    StringBuilder("[Status] = ").Append(res.StatusCode).Append(Environment.NewLine)
                        .Append("[TicksOffset] = ").Append(st.ElapsedTicks).Append(" ticks").ToString()
                logger.LogInformation(log)
        )

let initServices (services : IServiceCollection) (address: string) (loggerProvider: ILoggerProvider) =
    let apiKey = serviceSettings.ApiKey
        
    FbApp.createFromFile firebaseSettings.ProjectId firebaseSettings.CredentialsPath 
    |> services.AddSingleton<FbApp>
    |> ignore

    apiKey.Equals
    |> services.AddSingleton<ApiKeyValidator>
    |> ignore
    
    if(serviceSettings.HeartbeatMonitor) then
        startHeartbeatMonitor apiKey address loggerProvider