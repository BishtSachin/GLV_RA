namespace GoldLoanReappraisal.Data.Models
{
    public enum LoginResultStatus
    {
        Success,
        UserNotFound,
        InvalidPassword, // New
        AccountInactive,
        AccountLocked,     // New
        LoginExpired,
        ConcurrentLoginDetected,
        PasswordChangeRequired,
        Error
    }
}
