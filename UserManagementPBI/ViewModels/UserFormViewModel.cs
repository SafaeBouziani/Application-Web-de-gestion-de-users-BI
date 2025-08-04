// ViewModels/UserCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace UserManagementPBI.ViewModels
{
    public class UserFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string? UserName { get; set; }

        [Display(Name = "Role")]
        public string? BIUserRole { get; set; }

        [Display(Name = "Client")]
        public string? Client { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Mail { get; set; }

        [Display(Name = "View User")]
        public string? View_user { get; set; }
    }
}
