namespace FirebaseAdminUtil

open FirebaseAdmin
open FirebaseAdmin.Auth
open Google.Apis.Auth.OAuth2
open FSharp.Control.Tasks

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