module Server.Middleware

open System
open System.Text
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Server.Configurations

let validateApiKey: HttpHandler =
    let accessDenied  = setStatusCode 401 >=> text "Access Denied!"
    
    let validateApiKey (ctx : HttpContext) =
        let logger = ctx.GetLogger("Validate-Api-Key")
        match ctx.TryGetRequestHeader "X-API-Key" with
        | Some k ->
            let isGuid, key  = Guid.TryParse k
            let validate = ctx.GetService<ApiKeyValidator>()
            let isValid = isGuid && validate(key)
            logger.LogDebug($"Api key '{k}' is valid? {isValid}")
            isValid
        | None ->
            logger.LogDebug("No Api key on Request Headers")
            false
    
    authorizeRequest validateApiKey accessDenied
    
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
    