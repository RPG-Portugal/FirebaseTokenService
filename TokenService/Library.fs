namespace TokenService

open System
open System.Threading.Tasks
open Domain.Error
open Domain.Error
open FirebaseAdminUtil
open FSharp.Control.Tasks

module Service =
        
    let validateUserId (userId: uint64) = Result.Ok(userId)
    
    let createTokenForUserId app userId: Task<Result<string, ErrorCode>> = task {
        match userId |> validateUserId |> Result.map (string >> FbApp.createWithValidUserId app) with
        | Result.Ok(tokenTask) ->
            let! token = tokenTask
            return Result.Ok(token)
        | Result.Error(e) -> return e
    }
