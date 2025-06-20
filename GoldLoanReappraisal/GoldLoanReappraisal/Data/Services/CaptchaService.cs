// In Data/Services/CaptchaService.cs
using Dapper;
using GoldLoanReappraisal.Data.Models;
using Microsoft.Data.SqlClient;
// Add this using statement for logging
namespace GoldLoanReappraisal.Data.Services
{
    public class CaptchaService
    {
        private readonly IConfiguration _configuration;
        // Add a private field for the logger
        private readonly ILogger<CaptchaService> _logger;

        // Modify the constructor to accept the logger
        public CaptchaService(IConfiguration configuration, ILogger<CaptchaService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CaptchaItem?> GetRandomCaptchaAsync()
        {
            var sql = "SELECT TOP 1 Question, Answer FROM login_question ORDER BY NEWID()";

            _logger.LogInformation($"Testing serilogs: {sql}");

            using (var connection = new SqlConnection(_configuration.GetConnectionString("OrganisationConnection")))
            {
                try
                {
                    var captchaItem = await connection.QueryFirstOrDefaultAsync<CaptchaItem>(sql);
                    return captchaItem;
                }
                catch (Exception ex)
                {
                    // Use the logger to record the detailed exception
                    _logger.LogError(ex, "DATABASE ERROR: Failed to fetch CAPTCHA from the database.");
                    return null;
                }
            }
        }
    }
}