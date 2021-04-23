module Server.Handler
open System
open Giraffe
open Microsoft.AspNetCore.Http
open Server.Configurations
open TokenService
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging

type RequestBody = { UserId: string }


let handler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {        
        let logger: ILogger = ctx.GetLogger("CREATE-TOKEN-HANDLER")
        let app = ctx.GetService<FbApp>()
        let! body = ctx.BindJsonAsync<RequestBody>()
        let! res = Service.handle app body.UserId
        
        logger.LogInformation($"userId: {body.UserId}")
        
        let result =
            match res with
            | Ok(token) ->
                logger.LogInformation($"Generated Token Successfully!")
                ["token", token] 
            | Error(status) ->
                logger.LogError($"Request Failed with status: {status.ToString()}")
                ["error", status.ToString()]
                
        return! json (dict result) next ctx
    }
    
let validateApiKey: HttpHandler =
    let accessDenied  = setStatusCode 401 >=> text "Access Denied!"
    
    let validateApiKey (ctx : HttpContext) =
        match ctx.TryGetRequestHeader "X-API-Key" with
        | Some k ->
            let isGuid, key  = Guid.TryParse k
            let validate = ctx.GetService<ApiKeyValidator>()
                in isGuid && validate(key)
        | None -> false
    
    authorizeRequest validateApiKey accessDenied
    
let notFoundHandler: HttpHandler = RequestErrors.NOT_FOUND "Not Found"