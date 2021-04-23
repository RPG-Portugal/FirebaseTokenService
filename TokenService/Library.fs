namespace TokenService

open System
open FirebaseAdminUtil
open FSharp.Control.Tasks
open TokenService.Status

module Service =
        
    let validateUserId (userId: string) =
        let is64Bit, _ = UInt64.TryParse(userId)
        if is64Bit |> not
        then Result.Error(ErrorCode.UserIdNot64BitNumber)
        else Result.Ok(userId)
    
    let handle app userId = task {
        match userId |> validateUserId |> Result.map (FbApp.createWithValidUserId app) with
        | Result.Ok(tokenTask) ->
            let! token = tokenTask
            return Result.Ok(token)
        | Result.Error(e) -> return Error(e)
    }
