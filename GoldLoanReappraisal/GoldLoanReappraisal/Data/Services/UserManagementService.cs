using Dapper;
using GoldLoanReappraisal.Data.Models;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Dynamic;
using System.Data;

namespace GoldLoanReappraisal.Data.Services
{
    public class UserManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(IConfiguration configuration, ILogger<UserManagementService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PagedResult<VisibleUser>> GetVisibleUsersAsync(string requestorUserId, int pageNumber, int pageSize)
        {
            _logger.LogInformation("Fetching page {PageNumber} of visible users for requestor '{RequestorId}'.", pageNumber, requestorUserId);

            using (var connection = new OracleConnection(_configuration.GetConnectionString("OracleConnection")))
            {
                try
                {
                    var parameters = new OracleDynamicParameters();
                    parameters.Add("p_Requestor_UserId", requestorUserId, OracleDbType.NVarchar2, ParameterDirection.Input);
                    parameters.Add("p_PageNumber", pageNumber, OracleDbType.Int32, ParameterDirection.Input);
                    parameters.Add("p_PageSize", pageSize, OracleDbType.Int32, ParameterDirection.Input);
                    parameters.Add("p_Total_Records", dbType: OracleDbType.Decimal, direction: ParameterDirection.Output);
                    parameters.Add("p_UserList_Cursor", dbType: OracleDbType.RefCursor, direction: ParameterDirection.Output);

                    var users = await connection.QueryAsync<VisibleUser>(
                        "SP_GetVisibleUsers",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    // THIS IS THE FIX: Get the Oracle-specific decimal type first, then its value.
                    var oracleTotalRecords = parameters.Get<Oracle.ManagedDataAccess.Types.OracleDecimal>("p_Total_Records");
                    int totalRecords = (int)oracleTotalRecords.Value;

                    return new PagedResult<VisibleUser> { Items = users, TotalCount = totalRecords };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DATABASE ERROR: Failed to fetch visible users with stored procedure SP_GetVisibleUsers.");
                    return new PagedResult<VisibleUser>();
                }
            }
        }
    }
}