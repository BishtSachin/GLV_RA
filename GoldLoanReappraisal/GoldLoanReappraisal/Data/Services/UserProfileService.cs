// In Data/Services/UserProfileService.cs
using Dapper;
using GoldLoanReappraisal.Data.Models;
using Microsoft.Data.SqlClient; // Assuming this is for SQL Server
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

        // ... GetUserProfileAsync and GetUserProfileByIdAsync methods remain the same ...
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
        public async Task<UserProfileModel?> GetUserProfileByIdAsync(string userId)
        {
            _logger.LogInformation("Fetching profile for user '{UserId}'.", userId);

            // Ensure you replace 'VW_USER_PROFILE' with your actual view name if different
            var sql = "SELECT * FROM VW_USER_PROFILE WHERE UserId = :userId";

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


        // [SECURITY FIX & REFACTORED]
        public async Task<IEnumerable<BranchRegionZoneList>> GetAllBranchesAsync(string regionCode, string zoneCode)
        {
            // SQL query now uses parameters to prevent injection
            var sql = @"SELECT DISTINCT BRANCH_CODE AS Code, BRANCH_NAME AS Name 
                        FROM BRANCH_MASTER_VIEW 
                        WHERE (@ZoneCode = 'ALL' OR ZONE_CODE = @ZoneCode)
                          AND (@RegionCode = 'ALL' OR REGION_CODE = @RegionCode)
                        ORDER BY Name";
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("OrganisationConnection")))
                {
                    // Pass parameters safely as an anonymous object
                    return await connection.QueryAsync<BranchRegionZoneList>(sql, new { regionCode, zoneCode });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branch list for Zone: {ZoneCode}, Region: {RegionCode}", zoneCode, regionCode);
                return Enumerable.Empty<BranchRegionZoneList>(); // Return empty list on error
            }
        }

        // [SECURITY FIX & REFACTORED]
        public async Task<IEnumerable<BranchRegionZoneList>> GetAllRegionsAsync(string zoneCode)
        {
            // SQL query now uses a parameter to prevent injection
            var sql = @"SELECT DISTINCT region_code AS Code, region_name AS Name 
                        FROM REGION_MASTER_VIEW 
                        WHERE @ZoneCode = 'ALL' OR zone_code = @ZoneCode
                        ORDER BY Name";
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("OrganisationConnection")))
                {
                    // Pass the zoneCode parameter safely
                    return await connection.QueryAsync<BranchRegionZoneList>(sql, new { zoneCode });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching region list for Zone: {ZoneCode}", zoneCode);
                return Enumerable.Empty<BranchRegionZoneList>(); // Return empty list on error
            }
        }

        // [REFACTORED for consistency]
        public async Task<IEnumerable<BranchRegionZoneList>> GetAllZonesAsync()
        {
            var sql = "SELECT zone_code as Code, zone_name_eng as Name FROM ZONE_MASTER_VIEW ORDER BY zone_name_eng";
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("OrganisationConnection")))
                {
                    return await connection.QueryAsync<BranchRegionZoneList>(sql);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching zone list");
                return Enumerable.Empty<BranchRegionZoneList>(); // Return empty list on error
            }
        }
    }
}