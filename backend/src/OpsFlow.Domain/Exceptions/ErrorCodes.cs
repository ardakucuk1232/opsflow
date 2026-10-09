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

    public static class Users
    {
        public const string EmailNotVerified = "users.email_not_verified";
        public const string CannotModifySelf = "users.cannot_modify_self";
        public const string LastAdmin = "users.last_admin";
        public const string InvalidRoles = "users.invalid_roles";
        public const string InvitationAlreadyAccepted = "users.invitation_already_accepted";
    }

    public static class Roles
    {
        public const string SystemRoleLocked = "roles.system_role_locked";
        public const string InUse = "roles.in_use";
        public const string NameTaken = "roles.name_taken";
        public const string InvalidPermissions = "roles.invalid_permissions";
    }
}
