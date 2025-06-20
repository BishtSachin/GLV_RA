namespace GoldLoanReappraisal.Data.Models
{
    public class MenuItem
    {
        public required string Title { get; set; }
        public required string Href { get; set; }
        public string IconCssClass { get; set; } = "bi-arrow-right-short-nav-menu"; // A default icon

        // This is the key property for security. It lists the roles that can see this menu item.
        // If it's empty, it means all authenticated users can see it.
        public string[] AllowedUserTypes { get; set; } = [];
    }
}
