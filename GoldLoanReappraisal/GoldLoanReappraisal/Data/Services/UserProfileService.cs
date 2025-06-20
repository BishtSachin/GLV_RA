// In Data/Services/UserProfileService.cs
using Dapper;
using GoldLoanReappraisal.Data.Models;
using Oracle.ManagedDataAccess.Client;

namespace GoldLoanReappraisal.Data.Services
{
    public class UserProfileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(IConfiguration configuration, ILogger<UserProfileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<UserProfileModel?> GetUserProfileAsync(string userId)
        {
            // This query safely selects from your view and does not touch the password hash.
            var sql = @"SELECT 
                            UserId, UserType, UserStatus, LastLoginDate, BranchCode, BranchName, 
                            RegionCode, RegionName, ZoneCode, ZoneName, CreatedDate, CreatedBy, 
                            ModifiedDate, ModifiedBy 
                        FROM 
                            VW_User_Profile 
                        WHERE 
                            UserId = :userId";

            using (var connection = new OracleConnection(_configuration.GetConnectionString("OracleConnection")))
            {
                try
                {
                    var profile = await connection.QueryFirstOrDefaultAsync<UserProfileModel>(sql, new { userId });
                    return profile;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DATABASE ERROR: Failed to fetch profile for user '{UserId}'.", userId);
                    return null;
                }
            }
        }
    }
}