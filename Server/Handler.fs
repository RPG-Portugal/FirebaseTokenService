module Server.Handler
open Giraffe
open Microsoft.AspNetCore.Http
open Server.Configurations
open TokenService.Service
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging


let createTokenHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let logger: ILogger = ctx.GetLogger("Create-Token-Handler")
            let app = ctx.GetService<FbApp>()
            let! body = ctx.BindJsonAsync<{|UserId: string|}>()
            let! res = createTokenForUserId app body.UserId
             
            logger.LogInformation($"userId: {body.UserId}")
 
            return! match res with
                    | Ok(token) ->
                        logger.LogInformation($"Generated Token Successfully!")
                        ["token", token] 
                    | Error(status) ->
                        logger.LogError($"Request Failed with status: {status.ToString()}")
                        ["error", status.ToString()]
            |> dict
            |> fun res -> json res next ctx
        }
        
let notFoundHandler: HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            ctx.GetLogger("NotFoundHandler")
               .LogError($"Error 404 [PATH=\"{ctx.Request.Path.ToString()}\"]")
            return! (RequestErrors.NOT_FOUND "Not Found!") next ctx
        }
    