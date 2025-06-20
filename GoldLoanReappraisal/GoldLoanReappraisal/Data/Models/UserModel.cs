// In Data/Models/UserModel.cs
namespace GoldLoanReappraisal.Data.Models
{
    public class UserModel
    {
        // Only properties needed for login validation from the USERMASTER table
        public string USERID { get; set; }
        public string PASSWORDHASH { get; set; }
        public string USERSTATUS { get; set; }
        public DateTime? LASTLOGINDATE { get; set; }
        public int NOOFATTEMPTSLEFT { get; set; }
        public string? CURRENTSESSIONID { get; set; }
        public DateTime? SESSIONEXPIRY { get; set; }
    }
}