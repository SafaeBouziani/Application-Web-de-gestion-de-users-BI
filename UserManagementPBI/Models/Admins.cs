using Microsoft.AspNetCore.Identity;

namespace UserManagementPBI.Models
{
    public class Admins:IdentityUser
    {
        public string Nom { get; set; }
        public ICollection<Users>? Users { get; set; }
    }

}
