namespace GoldLoanReappraisal.Data.Models
{
    public class LoginResult
    {
        public LoginResultStatus Status { get; set; }
        public string? Message { get; set; }
        public string? SessionId { get; set; }
        public UserModel? User { get; set; }
        public int AttemptsLeft { get; set; } // <-- ADD THIS PROPERTY
    }
}