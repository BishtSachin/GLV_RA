using System.Security.Claims;
using GoldLoanReappraisal.Data.Models;
using GoldLoanReappraisal.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldLoanReappraisal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserValidationService _userValidationService;
        private readonly UserProfileService _userProfileService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserValidationService userValidationService, UserProfileService userProfileService, ILogger<AuthController> logger)
        {
            _userValidationService = userValidationService;
            _userProfileService = userProfileService;
            _logger = logger;
        }

        public class LoginModel
        {
            public string? UserId { get; set; }
            public string? Password { get; set; }
        }
        public class ChangePasswordModel { public string? NewPassword { get; set; } }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] LoginModel model)
        {
            if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.Password))
            {
                return LocalRedirect("/?status=InvalidCredentials");
            }

            var loginResult = await _userValidationService.ValidateUserLoginAsync(model.UserId, model.Password);
            _logger.LogInformation("API: User validation service returned status: {Status}", loginResult.Status);

            // --- THIS IS THE CORRECTED LOGIC BLOCK ---
            // First, check for any successful outcome (regular success OR password change needed)
            if (loginResult.Status == LoginResultStatus.Success || loginResult.Status == LoginResultStatus.PasswordChangeRequired)
            {
                // Both cases require us to fetch the profile and sign the user in.
                var userProfile = await _userProfileService.GetUserProfileAsync(model.UserId);
                if (userProfile == null)
                {
                    return LocalRedirect("/?status=InvalidCredentials");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userProfile.UserId),
                    new Claim(ClaimTypes.Role, userProfile.UserType),
                    new Claim("BranchName", userProfile.BranchName ?? ""),
                    new Claim("RegionName", userProfile.RegionName ?? ""),
                    new Claim("ZoneName", userProfile.ZoneName ?? ""),
                    new Claim("BranchCode", userProfile.BranchCode ?? ""),
                    new Claim("RegionCode", userProfile.RegionCode ?? ""),
                    new Claim("ZoneCode", userProfile.ZoneCode ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                _logger.LogInformation("API: SignInAsync completed for user '{UserId}'.", model.UserId);

                // Now, redirect to the correct page based on the specific status
                if (loginResult.Status == LoginResultStatus.PasswordChangeRequired)
                {
                    _logger.LogInformation("API: Redirecting user '{UserId}' to change password page.", model.UserId);
                    return LocalRedirect("/changepassword");
                }
                else // This means the status was Success
                {
                    _logger.LogInformation("API: Redirecting user '{UserId}' to dashboard.", model.UserId);
                    return LocalRedirect("/dashboard");
                }
            }
            else
            {
                // This block now correctly handles all other failure cases
                string errorStatus = loginResult.Status.ToString();
                int attempts = loginResult.AttemptsLeft;
                return LocalRedirect($"/?status={errorStatus}&attempts={attempts}");
            }
        }
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.Identity?.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                await _userValidationService.ClearUserSessionAsync(userId);
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect("/?status=LogoutSuccessful");
        }
    }
}