namespace FirebaseUtil

open System.Threading.Tasks
open FirebaseAdmin.Auth
open FirebaseAdmin
open Google.Apis.Auth.OAuth2
open FSharp.Control.Tasks
open Domain

module FbApp =
    
    let createFromFile projectId credentialsFilePath = 
        let cred = credentialsFilePath |> GoogleCredential.FromFile
            in AppOptions(Credential=cred, ProjectId=projectId)
               |> FirebaseApp.Create
    
        
    // token must be validated from elsewhere
    let createWithValidUserId (app: FirebaseApp) (userId: string) =        
        app
        |> FirebaseAuth.GetAuth 
        |> fun a -> a.CreateCustomTokenAsync userId
        
    let validateUserId (userId: uint64) = Result.Ok(userId)
    
    let createTokenForUserId app userId: Task<Result<string, Error.ErrorCode>> = task {
        match userId |> validateUserId |> Result.map (string >> createWithValidUserId app) with
        | Result.Ok(tokenTask) ->
            let! token = tokenTask
            return Result.Ok(token)
        | Result.Error(e) -> return e
    }