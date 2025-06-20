using GoldLoanReappraisal.Data.Models;
using System.Security.Claims;

namespace GoldLoanReappraisal.Data.Services
{
    public class NavigationMenuService
    {
        private readonly List<MenuItem> _allMenuItems =
        [
            new() {
                Title = "Dashboard",
                Href = "/dashboard",
                IconCssClass = "bi-house-door-fill-nav-menu",
                AllowedUserTypes = [] // Empty means all logged-in users can see it
            },
            new() {
                Title = "User Management",
                Href = "/user-management", // Placeholder URL as requested
                IconCssClass = "bi-shield-lock-fill-nav-menu",
                AllowedUserTypes = ["Admin","Region","Zone"] // Only users with the "Admin" role can see this
            },
            new() {
                Title = "Data Upload",
                Href = "#",
                IconCssClass = "bi-gem-nav-menu",
                AllowedUserTypes = ["Admin","Region","Zone"] // Users with EITHER "Appraiser" OR "Admin" role can see this
            },
            new() {
                Title = "Data View",
                Href = "#",
                IconCssClass = "bi-person-fill-nav-menu",
                AllowedUserTypes = ["Admin","Region","Zone"] // All logged-in users can see this
            }
        ];

        public IEnumerable<MenuItem> GetAllowedMenuItems(ClaimsPrincipal user)
        {
            // If the user is not authenticated, return an empty list.
            if (user.Identity?.IsAuthenticated != true)
            {
                return [];
            }

            var allowedItems = new List<MenuItem>();
            foreach (var item in _allMenuItems)
            {
                if (!item.AllowedUserTypes.Any() || item.AllowedUserTypes.Any(user.IsInRole))
                {
                    allowedItems.Add(item);
                }
            }
            return allowedItems;
        }
    }
}