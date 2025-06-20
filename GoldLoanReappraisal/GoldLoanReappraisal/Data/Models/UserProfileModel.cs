namespace GoldLoanReappraisal.Data.Models
{
    public class UserProfileModel
    {
        public string UserId { get; set; }
        public string UserType { get; set; }
        public string UserStatus { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string RegionCode { get; set; }
        public string RegionName { get; set; }
        public string ZoneCode { get; set; }
        public string ZoneName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
