using System.ComponentModel.DataAnnotations;

namespace GoldLoanReappraisal.Data.Models
{
    public class ChangePasswordFormModel
    {
        [Required(ErrorMessage = "New Password is required.")]
        [StringLength(15, ErrorMessage = "Password must be between 8 and 15 characters.", MinimumLength = 8)]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
