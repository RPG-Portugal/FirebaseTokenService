module Domain.Error

type ErrorMessage = string

type ErrorCode =
    | InvalidJsonPayload
    | InvalidUserId
    | UserError
    | UnknownError

type ErrorData = { ErrorCode: ErrorCode; ErrorMessage:ErrorMessage }
let [<Literal>] UNKNOWN_ERROR_MESSAGE = "Internal Error Message"