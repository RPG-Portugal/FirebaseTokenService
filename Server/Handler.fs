module Server.Handler
open System
open System.Text
open Giraffe
open Microsoft.AspNetCore.Http
open Server.Configurations
open TokenService
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging

type RequestBody = { UserId: string }

let logRequest: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let nl = Environment.NewLine
        let logger = ctx.GetLogger("Request-Logger")
        let req = ctx.Request
        
        let log: StringBuilder = StringBuilder(nl)
        log.Append("--- Logging Request ---").Append(nl) |> ignore
        log.Append("METHOD  = ").Append(req.Method).Append(nl) |> ignore
        log.Append("PATH    = ").Append(req.Path.ToString()).Append(nl) |> ignore
        log.Append("HEADERS:").Append(nl).Append(nl) |> ignore
        for h in req.Headers do
            log.Append($"[{h.Key}] = [{h.Value}]").Append(nl) |> ignore
        log.Append(nl).Append("-----------------------") |> ignore
        logger.LogInformation(log.ToString())
        next ctx   
    
let createTokenHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {        
        let logger: ILogger = ctx.GetLogger("Create Token Handler")
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
    
let notFoundHandler: HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let logger = ctx.GetLogger("NotFoundHandler")
            logger.LogError($"Error 404 [PATH=\"{ctx.Request.Path.ToString()}\"]")
            return! (RequestErrors.NOT_FOUND "Not Found") next ctx
        }
    