module Server.Logging

open System
open System.IO
open FSharp.Data
open Microsoft.Extensions.Logging

let private config = JsonProvider<"Resources/Logging.json">.GetSample()

let getTimeZoneDateTimeNow() =
    "GMT Standard Time"
    |> TimeZoneInfo.FindSystemTimeZoneById
    |> fun timezone -> TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone)
 
type CustomLogger(categoryName: string) =
    let logLevels =
        config.LogLevels.Allowed
        |> Array.map LogLevel.Parse
        |> Set.ofArray
    
    let path = config.Path
    let logModes = config.LogModes |> set
    
    let getFileName() =
        let today = getTimeZoneDateTimeNow().ToString("dd_MM_yyyy")
            in $"{path}log_{today}.log"
    
    let isEnabled = logLevels.Contains
    
    let getLogLevelColor (lvl: LogLevel)=
        config.LogLevels.Colors.JsonValue.Properties()
        |> dict
        |> (fun colors -> lvl.ToString() |> colors.TryGetValue)
        |> (fun (found,color) ->
            if found then
                (color.AsString() |> ConsoleColor.Parse)
                else Console.ForegroundColor)

    (* TODO: low hanging fruit - clean this code to be more readable
       Maybe use an object to get the fields as immutable state
       ex: TextFormat.{ConsoleWrite(), FileWrite()} *)
    let logConsole (date: string) (logLevel: LogLevel) (logLvlTxt: string) (exceptionSeparator: string) (arrow: string) (eventTxt: string) (stateTxt: string) (exceptionText: unit -> string) (eventId: EventId) ``exception`` =
        let original = Console.ForegroundColor
        Console.ForegroundColor <- ConsoleColor.Blue
        Console.Write(categoryName)
        Console.ForegroundColor <- original
        Console.ForegroundColor <- getLogLevelColor logLevel
        Console.WriteLine(logLvlTxt)
        Console.ForegroundColor <- ConsoleColor.Yellow
        Console.Write(date)
        Console.ForegroundColor <- original
        Console.Write(arrow)
        if eventId.Id <> 0 then
            Console.ForegroundColor <- ConsoleColor.Cyan
            Console.Write(eventTxt)
            Console.ForegroundColor <- original
        Console.WriteLine()
        Console.WriteLine(stateTxt)
        if ``exception`` <> null then
            Console.ForegroundColor <- ConsoleColor.Red
            Console.WriteLine(exceptionSeparator)
            Console.ForegroundColor <- original
            Console.WriteLine(exceptionText())
            Console.ForegroundColor <- ConsoleColor.Red
            Console.WriteLine(exceptionSeparator)
            Console.ForegroundColor <- original
    
    let logFile (file: StreamWriter) (date: string) (logLvlTxt: string) (exceptionSeparator: string) (arrow: string) (eventTxt: string) (stateTxt: string) (exceptionText: unit -> string) (eventId: EventId) ``exception`` =
        file.Write(categoryName)
        file.WriteLine(logLvlTxt)
        file.Write(date)
        file.Write(arrow)
        if eventId.Id <> 0 then
            file.Write(eventTxt)
        file.WriteLine()
        file.WriteLine(stateTxt)
        if ``exception`` <> null then
            file.WriteLine(exceptionSeparator)
            file.WriteLine(exceptionText())
            file.WriteLine(exceptionSeparator)
                        
    interface ILogger with
        member this.BeginScope _ = Unchecked.defaultof<IDisposable>
        member this.IsEnabled logLevel = isEnabled logLevel
        
        member this.Log(logLevel, eventId, state, ``exception``, _) =
            if isEnabled logLevel then
                let date = getTimeZoneDateTimeNow().ToString("o")
                let logLvlTxt = $" [{logLevel}]"
                let exceptionSeparator = ":::: Exception ::::"
                let arrow = " ===> "
                let eventTxt = $"[{eventId.Name}({eventId.Id})] "
                let stateTxt = state.ToString()
                let exceptionText() = ``exception``.ToString()

                if logModes.Contains "Console" then
                    logConsole date logLevel logLvlTxt exceptionSeparator arrow eventTxt stateTxt exceptionText eventId ``exception``
                if logModes.Contains "File" then
                    use file = getFileName() |> File.AppendText
                    logFile file date logLvlTxt exceptionSeparator arrow eventTxt stateTxt exceptionText eventId ``exception``

let loggerProvider() =
    { new ILoggerProvider with
        member this.CreateLogger(categoryName) = CustomLogger(categoryName) :> ILogger
        member this.Dispose() = () }