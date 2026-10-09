namespace OpsFlow.Domain.Exceptions;

public static class ErrorCodes
{
    public const string BadRequest = "bad_request";
    public const string ValidationFailed = "validation_failed";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string BusinessRuleViolation = "business_rule_violation";
    public const string TooManyRequests = "too_many_requests";
    public const string InternalError = "internal_error";
    public const string InsufficientPrivileges = "insufficient_privileges";

    public static class Auth
    {
        public const string InvalidCredentials = "auth.invalid_credentials";
        public const string InvalidRefreshToken = "auth.invalid_refresh_token";
        public const string AccountDisabled = "auth.account_disabled";
        public const string EmailAlreadyInUse = "auth.email_already_in_use";
        public const string InvalidToken = "auth.invalid_token";
    }
}
