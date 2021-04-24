module Server.Handler
open Giraffe
open Microsoft.AspNetCore.Http
open Server.Configurations
open TokenService
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging

type RequestBody = { UserId: string }

let createTokenHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {        
        let logger: ILogger = ctx.GetLogger("Create-Token-Handler")
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
        
let notFoundHandler: HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let logger = ctx.GetLogger("NotFoundHandler")
            logger.LogError($"Error 404 [PATH=\"{ctx.Request.Path.ToString()}\"]")
            return! (RequestErrors.NOT_FOUND "Not Found!") next ctx
        }
    