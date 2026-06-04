using System.ComponentModel.DataAnnotations;

namespace WMSClean.Models
{
    public class UserRoleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool EmailConfirmed { get; set; }
    }

    public class UserDetailsViewModel : UserRoleViewModel
    {
        public List<Product> RecentProducts { get; set; } = new();
    }
}