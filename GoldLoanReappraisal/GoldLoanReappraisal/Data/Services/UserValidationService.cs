using Dapper;
using GoldLoanReappraisal.Data.Models;
using Oracle.ManagedDataAccess.Client;

namespace GoldLoanReappraisal.Data.Services
{
    public class UserValidationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserValidationService> _logger;
        private const int SessionTimeoutInMinutes = 20; // Define session length

        public UserValidationService(IConfiguration configuration, ILogger<UserValidationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResult> ValidateUserLoginAsync(string userId, string password)
        {
            // This query now correctly uses the USERMASTER table for all auth-related data
            var sql = "SELECT USERID, PASSWORDHASH, USERSTATUS, LASTLOGINDATE, NOOFATTEMPTSLEFT, CURRENTSESSIONID, SESSIONEXPIRY FROM USERMASTER WHERE USERID = :userId";

            using (var connection = new OracleConnection(_configuration.GetConnectionString("OracleConnection")))
            {
                try
                {
                    var user = await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { userId });

                    if (user == null) return new LoginResult { Status = LoginResultStatus.UserNotFound };
                    if (user.USERSTATUS == "Locked") return new LoginResult { Status = LoginResultStatus.AccountLocked, AttemptsLeft = 0 };
                    if (user.USERSTATUS == "Inactive") return new LoginResult { Status = LoginResultStatus.AccountInactive };

                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PASSWORDHASH);

                    if (!isPasswordValid)
                    {
                        _logger.LogWarning("Invalid password attempt for user '{UserId}'.", userId);

                        int newAttemptsLeft = user.NOOFATTEMPTSLEFT - 1;
                        if (newAttemptsLeft <= 0)
                        {
                            _logger.LogWarning("User '{UserId}' has no attempts left. Locking account.", userId);
                            await connection.ExecuteAsync("UPDATE USERMASTER SET USERSTATUS = 'Locked', NOOFATTEMPTSLEFT = 0 WHERE USERID = :userId", new { userId });
                            return new LoginResult { Status = LoginResultStatus.AccountLocked, AttemptsLeft = 0 };
                        }
                        else
                        {
                            await connection.ExecuteAsync("UPDATE USERMASTER SET NOOFATTEMPTSLEFT = :newAttemptsLeft WHERE USERID = :userId", new { newAttemptsLeft, userId });
                            return new LoginResult { Status = LoginResultStatus.InvalidPassword, AttemptsLeft = newAttemptsLeft };
                        }
                    }

                    // --- NEW LOGIC ---
                    // If password is valid, check if a password change is required
                    if (user.USERSTATUS == "PendingActivation")
                    {
                        _logger.LogInformation("User '{UserId}' has 'PendingActivation' status. Forcing password change.", userId);
                        return new LoginResult { Status = LoginResultStatus.PasswordChangeRequired, User = user };
                    }


                    if (!string.IsNullOrEmpty(user.CURRENTSESSIONID) && user.SESSIONEXPIRY.HasValue && user.SESSIONEXPIRY.Value > DateTime.UtcNow)
                    {
                        _logger.LogWarning("Concurrent login detected for user '{UserId}'.", userId);
                        return new LoginResult { Status = LoginResultStatus.ConcurrentLoginDetected };
                    }

                    if (user.LASTLOGINDATE.HasValue && (DateTime.Now - user.LASTLOGINDATE.Value).TotalDays > 30)
                    {
                        _logger.LogWarning("Login expired for user '{UserId}'. Last login was on {LastLoginDate}. Setting status to Inactive.", userId, user.LASTLOGINDATE.Value);

                        // --- ADDED MISSING CODE ---
                        // This executes the command to update the user's status in the database.
                        await connection.ExecuteAsync("UPDATE USERMASTER SET USERSTATUS = 'Inactive' WHERE USERID = :userId", new { userId });

                        return new LoginResult { Status = LoginResultStatus.LoginExpired };
                    }

                    // All checks passed! Generate new session and update the database.
                    var newSessionId = Guid.NewGuid().ToString();
                    var newSessionExpiry = DateTime.UtcNow.AddMinutes(SessionTimeoutInMinutes);

                    _logger.LogInformation("Login success for '{UserId}'. Assigning new session ID {SessionId}", userId, newSessionId);

                    var successUpdateSql = @"UPDATE UserMaster 
                                           SET 
                                               LASTLOGINDATE = :lastLogin, 
                                               NOOFATTEMPTSLEFT = 5, 
                                               CURRENTSESSIONID = :sessionId, 
                                               SESSIONEXPIRY = :sessionExpiry 
                                           WHERE 
                                               USERID = :userId";

                    await connection.ExecuteAsync(successUpdateSql, new
                    {
                        lastLogin = DateTime.Now,
                        sessionId = newSessionId,
                        sessionExpiry = newSessionExpiry,
                        userId
                    });

                    // --- ADDED MISSING CODE ---
                    // Return a success result that includes the User object for the AuthController
                    return new LoginResult { Status = LoginResultStatus.Success, SessionId = newSessionId, User = user };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DATABASE ERROR during user validation for user '{UserId}'.", userId);
                    return new LoginResult { Status = LoginResultStatus.AccountInactive, Message = "A database error occurred." };
                }
            }
        }
        // --- NEW METHOD ---
        public async Task<bool> UpdatePasswordAsync(string userId, string newPassword)
        {
            _logger.LogInformation("Attempting to update password for user '{UserId}'.", userId);

            // Securely hash the new password
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // This query updates the hash, status, and resets login attempts.
            // It also shifts the password history.
            var sql = @"UPDATE USERMASTER SET
                            PASSWORDHASH = :newHash,
                            USERSTATUS = 'Active',
                            NOOFATTEMPTSLEFT = 5,
                            LASTPASSWORD5 = LASTPASSWORD4,
                            LASTPASSWORD4 = LASTPASSWORD3,
                            LASTPASSWORD3 = LASTPASSWORD2,
                            LASTPASSWORD2 = LASTPASSWORD1,
                            LASTPASSWORD1 = PASSWORDHASH
                        WHERE USERID = :userId";

            using (var connection = new OracleConnection(_configuration.GetConnectionString("OracleConnection")))
            {
                try
                {
                    var rowsAffected = await connection.ExecuteAsync(sql, new { newHash = newPasswordHash, userId });
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DATABASE ERROR: Failed to update password for user '{UserId}'.", userId);
                    return false;
                }
            }
        }
        public async Task ClearUserSessionAsync(string userId)
        {
            _logger.LogInformation("Clearing session data for user '{UserId}' from the database.", userId);
            var sql = "UPDATE USERMASTER SET CURRENTSESSIONID = NULL, SESSIONEXPIRY = NULL WHERE USERID = :userId";

            using (var connection = new OracleConnection(_configuration.GetConnectionString("OracleConnection")))
            {
                try
                {
                    await connection.ExecuteAsync(sql, new { userId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DATABASE ERROR: Failed to clear session for user '{UserId}'.", userId);
                }
            }
        }
    }
}