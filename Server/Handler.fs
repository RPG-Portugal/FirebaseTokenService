module Server.Handler
open System
open Domain.Error
open Giraffe
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open FirebaseUtil
open Server.Configurations
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging

type ErrorResponse = {Error: ErrorData; StatusCode: option<int>}
type RequestResult<'T> = Result<'T,ErrorResponse>

module RequestResult =
       let createError (code: ErrorCode, message: ErrorMessage, statusCode: option<int>) =
        {Error =
            {ErrorCode=code;
             ErrorMessage=message};
         StatusCode=statusCode}
        
       let error<'T> (code: ErrorCode, message: ErrorMessage, statusCode: option<int>): RequestResult<'T> =
           createError (code, message, statusCode)    |> Error
    
let createTokenHandler: HttpHandler = 
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let respond res = json res next ctx
        let respondError (error: ErrorResponse) =
            Option.iter ctx.SetStatusCode error.StatusCode
            error.Error |>(fun e -> {|ErrorCode=e.ErrorCode.ToString(); ErrorMessage=e.ErrorMessage|})|> respond 
        
        task {
            let logger: ILogger = ctx.GetLogger("Create-Token-Handler")
            let! res = task {
                try
                    let app = ctx.GetService<FbApp>()
                    let! body = ctx.BindJsonAsync<{|UserId: uint64|}>()
                    logger.LogInformation($"userId: {body.UserId}")
                    
                    let! token = FbApp.createTokenForUserId app body.UserId
                        in return token |> Result.mapError (fun e -> RequestResult.createError(e, "Check error code", StatusCodes.Status500InternalServerError |> Some))
                with
                | :? JsonSerializationException ->
                    return RequestResult.error(ErrorCode.InvalidUserId, "UserId must be 64 bit unsigned int (Snowflake)", StatusCodes.Status400BadRequest |> Some)
                | :? JsonReaderException ->
                    return RequestResult.error(ErrorCode.InvalidJsonPayload, "Body is malformed", StatusCodes.Status400BadRequest |> Some)
                | _ ->
                    return RequestResult.error (ErrorCode.UnknownError, "Internal Server Error", Some(StatusCodes.Status500InternalServerError))
            }
             
            return! match res with
                    | Ok(token) ->
                        logger.LogInformation($"Generated Token Successfully!")
                        respond {| Token=token |} 
                    | Error(error) ->
                        logger.LogError($"Request Failed with status:{Environment.NewLine}{error}")
                        respondError error
        }
        
let notFoundHandler: HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            ctx.GetLogger("NotFoundHandler")
               .LogError($"Error 404 [PATH=\"{ctx.Request.Path.ToString()}\"]")
            return! (RequestErrors.NOT_FOUND "Not Found!") next ctx
        }
    